using CarTrack.Api;
using CarTrack.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace CarTrack.Modules.Expense;

public sealed class ExpenseModule : IModule
{
    public const string MigrationsHistoryTable = "__EFMigrationsHistory_Expense";
    public const string ProbeTable = "ExpenseCategories";

    public string Name => "expense";

    public void AddModule(IHostApplicationBuilder builder)
    {
        builder.Services.TryAddSingleton<AuditableEntityInterceptor>();

        builder.AddModuleNpgsqlDbContext<ExpenseDbContext>(
            "cartrack",
            MigrationsHistoryTable,
            (sp, options) =>
            {
                var interceptor = sp.GetRequiredService<AuditableEntityInterceptor>();
                options.AddInterceptors(interceptor);
            });

        builder.Services.AddScoped<IExpenseApprovalService, ExpenseApprovalService>();
        builder.Services.AddScoped<IExpenseCategoryService, ExpenseCategoryService>();
        builder.Services.AddScoped<IExpenseReportService, ExpenseReportService>();
    }

    public void MapEndpoints(IEndpointRouteBuilder endpoints) =>
        endpoints.MapGroup("/api/expense").MapExpenseEndpoints();

    public async Task MigrateAsync(IServiceProvider services, CancellationToken cancellationToken = default)
    {
        await using var scope = services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ExpenseDbContext>();
        await db.MigrateModuleAsync(MigrationsHistoryTable, ProbeTable, cancellationToken);
    }

    public async Task SeedAsync(IServiceProvider services, CancellationToken cancellationToken = default)
    {
        await using var scope = services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ExpenseDbContext>();
        await ExpenseSeeder.SeedAsync(db, cancellationToken);
    }
}
