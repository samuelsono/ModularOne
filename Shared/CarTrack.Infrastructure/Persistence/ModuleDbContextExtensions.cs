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
    /// When the history table is empty and every probe table already exists, pending
    /// migrations are baselined (recorded without executing) for legacy databases.
    /// If only some probe tables exist, startup fails with a partial-schema error so a
    /// dirty Identity/module schema is not half-applied again.
    /// A PostgreSQL advisory lock serializes concurrent app instances during migrate.
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
        var existingProbes = new List<string>(probeTables.Count);
        foreach (var probeTable in probeTables)
        {
            if (await TableExistsAsync(dbContext, probeTable, cancellationToken))
            {
                existingProbes.Add(probeTable);
            }
        }

        if (applied.Count == 0 && existingProbes.Count > 0)
        {
            if (existingProbes.Count != probeTables.Count)
            {
                var missing = probeTables.Except(existingProbes, StringComparer.Ordinal).ToList();
                throw new InvalidOperationException(
                    $"Database is partially migrated for '{historyTable}'. " +
                    $"Found existing table(s): {string.Join(", ", existingProbes)}. " +
                    $"Missing: {string.Join(", ", missing)}. " +
                    "Clear the schema before a fresh deploy, e.g. " +
                    "DROP SCHEMA public CASCADE; CREATE SCHEMA public; " +
                    "GRANT ALL ON SCHEMA public TO CURRENT_USER; GRANT ALL ON SCHEMA public TO public;");
            }

            await EnsureHistoryTableAsync(dbContext, historyTable, cancellationToken);
            var productVersion = ResolveProductVersion();

            var insertSql =
                "INSERT INTO \"" + historyTable + "\" (\"MigrationId\", \"ProductVersion\") VALUES ({0}, {1})";

            foreach (var migrationId in pending)
            {
                await dbContext.Database.ExecuteSqlRawAsync(
                    insertSql,
                    new object[] { migrationId, productVersion },
                    cancellationToken);
            }

            return;
        }

        await dbContext.Database.MigrateAsync(cancellationToken);
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
            await using var command = connection.CreateCommand();
            command.CommandText =
                """
                SELECT EXISTS (
                    SELECT 1
                    FROM information_schema.tables
                    WHERE table_schema = 'public'
                      AND table_name = @tableName
                )
                """;

            var parameter = command.CreateParameter();
            parameter.ParameterName = "tableName";
            parameter.Value = tableName;
            command.Parameters.Add(parameter);

            var result = await command.ExecuteScalarAsync(cancellationToken);
            return result is true;
        }
        finally
        {
            if (shouldClose)
            {
                await dbContext.Database.CloseConnectionAsync();
            }
        }
    }

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
