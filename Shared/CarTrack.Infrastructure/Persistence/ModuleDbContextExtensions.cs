using System.Data.Common;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace CarTrack.Infrastructure.Persistence;

public static partial class ModuleDbContextExtensions
{
    public static void AddModuleNpgsqlDbContext<TContext>(
        this IHostApplicationBuilder builder,
        string connectionName,
        string migrationsHistoryTable,
        Action<IServiceProvider, DbContextOptionsBuilder>? configure = null)
        where TContext : DbContext
    {
        builder.Services.AddDbContext<TContext>((sp, options) =>
        {
            var configuration = sp.GetRequiredService<IConfiguration>();
            var connectionString = configuration.GetConnectionString(connectionName)
                ?? throw new InvalidOperationException($"Connection string '{connectionName}' is missing.");
            options.UseNpgsql(connectionString, npgsql =>
                npgsql.MigrationsHistoryTable(migrationsHistoryTable));
            configure?.Invoke(sp, options);
        });
    }

    /// <summary>
    /// Applies pending migrations for a module.
    /// </summary>
    /// <remarks>
    /// When probe tables already exist but history is empty or missing early migrations
    /// (common on live DBs after modularization / history rewrites), pending migrations
    /// that precede the first applied assembly migration are baselined (recorded without
    /// executing) so CREATE TABLE is not re-run. A PostgreSQL advisory lock serializes
    /// concurrent app instances during migrate.
    /// </remarks>
    public static Task MigrateModuleAsync(
        this DbContext dbContext,
        string historyTable,
        string probeTable,
        CancellationToken cancellationToken = default) =>
        MigrateModuleAsync(dbContext, historyTable, [probeTable], cancellationToken);

    /// <inheritdoc cref="MigrateModuleAsync(DbContext, string, string, CancellationToken)"/>
    public static async Task MigrateModuleAsync(
        this DbContext dbContext,
        string historyTable,
        IReadOnlyList<string> probeTables,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(dbContext);
        ArgumentException.ThrowIfNullOrWhiteSpace(historyTable);
        ArgumentNullException.ThrowIfNull(probeTables);
        if (probeTables.Count == 0)
        {
            throw new ArgumentException("At least one probe table is required.", nameof(probeTables));
        }

        EnsureSafeSqlIdentifier(historyTable);
        foreach (var probeTable in probeTables)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(probeTable);
            EnsureSafeSqlIdentifier(probeTable);
        }

        await using var migrationLock = await PostgresAdvisoryLock.AcquireAsync(
            dbContext,
            historyTable,
            cancellationToken);

        var pending = (await dbContext.Database.GetPendingMigrationsAsync(cancellationToken)).ToList();
        if (pending.Count == 0)
        {
            return;
        }

        var applied = (await dbContext.Database.GetAppliedMigrationsAsync(cancellationToken)).ToList();
        var allMigrations = dbContext.Database.GetMigrations().ToList();
        var existingProbes = new List<string>(probeTables.Count);
        foreach (var probeTable in probeTables)
        {
            if (await TableExistsAsync(dbContext, probeTable, cancellationToken))
            {
                existingProbes.Add(probeTable);
            }
        }

        if (existingProbes.Count > 0 && existingProbes.Count != probeTables.Count && applied.Count == 0)
        {
            var missing = probeTables.Except(existingProbes, StringComparer.Ordinal).ToList();
            throw new InvalidOperationException(
                $"Database is partially migrated for '{historyTable}'. " +
                $"Found existing table(s): {string.Join(", ", existingProbes)}. " +
                $"Missing: {string.Join(", ", missing)}. " +
                "For an empty environment wipe public (see docs/deploy/postgres/migration-recovery.md). " +
                "For a live database with user data, baseline history instead of re-running CREATE.");
        }

        // Schema already present for this module: baseline history gaps so EF does not
        // re-execute CREATE for tables like AspNetRoles / CarTrackSettings (42P07).
        if (existingProbes.Count == probeTables.Count)
        {
            var pendingSet = pending.ToHashSet(StringComparer.Ordinal);
            var appliedSet = applied.ToHashSet(StringComparer.Ordinal);
            var firstMigration = allMigrations.FirstOrDefault();
            var initialStillPending = firstMigration is not null && pendingSet.Contains(firstMigration);
            var firstPendingIndex = allMigrations.FindIndex(pendingSet.Contains);

            // Catch-up: probe tables exist but history is empty, missing Initial*, or only
            // partially recorded (e.g. InitialAuth inserted manually while AddCarTrackSettings
            // and the rest of the monolith chain are still pending). Baseline the pending
            // suffix when it starts well before the tip of the migration chain so brand-new
            // tip migrations (last few) still run via MigrateAsync.
            const int forwardMigrateTipWindow = 5;
            var isLegacyCatchUp = applied.Count == 0
                || initialStillPending
                || (firstPendingIndex >= 0
                    && firstPendingIndex < Math.Max(0, allMigrations.Count - forwardMigrateTipWindow));

            if (isLegacyCatchUp)
            {
                await BaselineMigrationsAsync(
                    dbContext,
                    historyTable,
                    pending,
                    cancellationToken);

                pending = (await dbContext.Database.GetPendingMigrationsAsync(cancellationToken)).ToList();
                if (pending.Count == 0)
                {
                    return;
                }
            }
        }

        await dbContext.Database.MigrateAsync(cancellationToken);
    }

    private static async Task BaselineMigrationsAsync(
        DbContext dbContext,
        string historyTable,
        IReadOnlyList<string> migrationIds,
        CancellationToken cancellationToken)
    {
        await EnsureHistoryTableAsync(dbContext, historyTable, cancellationToken);
        var productVersion = ResolveProductVersion();
        var insertSql =
            "INSERT INTO \"" + historyTable + "\" (\"MigrationId\", \"ProductVersion\") " +
            "VALUES ({0}, {1}) ON CONFLICT (\"MigrationId\") DO NOTHING";

        foreach (var migrationId in migrationIds)
        {
            await dbContext.Database.ExecuteSqlRawAsync(
                insertSql,
                [migrationId, productVersion],
                cancellationToken);
        }
    }

    private static string ResolveProductVersion()
    {
        var version = typeof(DbContext).Assembly.GetName().Version;
        return version is null ? "10.0.8" : $"{version.Major}.{version.Minor}.{version.Build}";
    }

    private static async Task EnsureHistoryTableAsync(
        DbContext dbContext,
        string historyTable,
        CancellationToken cancellationToken)
    {
        if (await TableExistsAsync(dbContext, historyTable, cancellationToken))
        {
            return;
        }

        var createSql =
            "CREATE TABLE \"" + historyTable + "\" (" +
            "\"MigrationId\" character varying(150) NOT NULL, " +
            "\"ProductVersion\" character varying(32) NOT NULL, " +
            "CONSTRAINT \"PK_" + historyTable + "\" PRIMARY KEY (\"MigrationId\"));";

        await dbContext.Database.ExecuteSqlRawAsync(createSql, cancellationToken);
    }

    private static async Task<bool> TableExistsAsync(
        DbContext dbContext,
        string tableName,
        CancellationToken cancellationToken)
    {
        var connection = dbContext.Database.GetDbConnection();
        var shouldClose = connection.State != System.Data.ConnectionState.Open;
        if (shouldClose)
        {
            await dbContext.Database.OpenConnectionAsync(cancellationToken);
        }

        try
        {
            // Prefer to_regclass with a quoted identifier so PascalCase EF tables
            // (e.g. "AspNetRoles") are found reliably on PostgreSQL.
            await using var command = connection.CreateCommand();
            command.CommandText = """SELECT to_regclass(@qualified) IS NOT NULL""";

            var parameter = command.CreateParameter();
            parameter.ParameterName = "qualified";
            parameter.Value = "public.\"" + tableName + "\"";
            command.Parameters.Add(parameter);

            var result = await command.ExecuteScalarAsync(cancellationToken);
            return IsTruthyDbScalar(result);
        }
        finally
        {
            if (shouldClose)
            {
                await dbContext.Database.CloseConnectionAsync();
            }
        }
    }

    private static bool IsTruthyDbScalar(object? result) =>
        result switch
        {
            null => false,
            bool value => value,
            int value => value != 0,
            long value => value != 0,
            _ when result == DBNull.Value => false,
            _ => Convert.ToBoolean(result),
        };

    private static void EnsureSafeSqlIdentifier(string identifier)
    {
        if (!SqlIdentifierRegex().IsMatch(identifier))
        {
            throw new ArgumentException(
                $"Invalid SQL identifier '{identifier}'. Only letters, digits, and underscores are allowed.",
                nameof(identifier));
        }
    }

    [GeneratedRegex("^[A-Za-z_][A-Za-z0-9_]*$")]
    private static partial Regex SqlIdentifierRegex();
}

/// <summary>
/// Session-scoped Postgres advisory lock so only one process migrates a history table at a time.
/// </summary>
file sealed class PostgresAdvisoryLock : IAsyncDisposable
{
    private readonly DbConnection _connection;
    private readonly long _key;
    private bool _released;

    private PostgresAdvisoryLock(DbConnection connection, long key)
    {
        _connection = connection;
        _key = key;
    }

    public static async Task<PostgresAdvisoryLock> AcquireAsync(
        DbContext dbContext,
        string historyTable,
        CancellationToken cancellationToken)
    {
        var connection = dbContext.Database.GetDbConnection();
        if (connection.State != System.Data.ConnectionState.Open)
        {
            await dbContext.Database.OpenConnectionAsync(cancellationToken);
        }

        // Stable key per history table (must fit in signed bigint for pg_advisory_lock(bigint)).
        var key = unchecked((long)(uint)StringComparer.Ordinal.GetHashCode(historyTable));

        await using (var command = connection.CreateCommand())
        {
            command.CommandText = "SELECT pg_advisory_lock(@key)";
            var parameter = command.CreateParameter();
            parameter.ParameterName = "key";
            parameter.Value = key;
            command.Parameters.Add(parameter);
            await command.ExecuteScalarAsync(cancellationToken);
        }

        return new PostgresAdvisoryLock(connection, key);
    }

    public async ValueTask DisposeAsync()
    {
        if (_released)
        {
            return;
        }

        _released = true;

        if (_connection.State != System.Data.ConnectionState.Open)
        {
            return;
        }

        await using var command = _connection.CreateCommand();
        command.CommandText = "SELECT pg_advisory_unlock(@key)";
        var parameter = command.CreateParameter();
        parameter.ParameterName = "key";
        parameter.Value = _key;
        command.Parameters.Add(parameter);
        await command.ExecuteScalarAsync();
    }
}
