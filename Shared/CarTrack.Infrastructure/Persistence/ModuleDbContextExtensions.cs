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
    /// Applies pending migrations. If the probe table already exists and no migrations
    /// are applied yet, baseline by inserting history rows without executing (existing DBs).
    /// </summary>
    public static async Task MigrateModuleAsync(
        this DbContext dbContext,
        string historyTable,
        string probeTable,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(dbContext);
        ArgumentException.ThrowIfNullOrWhiteSpace(historyTable);
        ArgumentException.ThrowIfNullOrWhiteSpace(probeTable);
        EnsureSafeSqlIdentifier(historyTable);
        EnsureSafeSqlIdentifier(probeTable);

        var pending = (await dbContext.Database.GetPendingMigrationsAsync(cancellationToken)).ToList();
        if (pending.Count == 0)
        {
            return;
        }

        var applied = (await dbContext.Database.GetAppliedMigrationsAsync(cancellationToken)).ToList();
        var probeExists = await TableExistsAsync(dbContext, probeTable, cancellationToken);

        if (probeExists && applied.Count == 0)
        {
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
