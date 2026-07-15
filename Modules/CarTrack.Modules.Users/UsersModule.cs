using CarTrack.Api;
using CarTrack.Infrastructure.Persistence;
using CarTrack.Server.Auth; // JwtOptions (Identity)
using CarTrack.Server.Users.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace CarTrack.Modules.Users;

public sealed class UsersModule : IModule
{
    public const string MigrationsHistoryTable = "__EFMigrationsHistory_Users";
    public const string ProbeTable = "StaffProfiles";

    public string Name => "users";

    public void AddModule(IHostApplicationBuilder builder)
    {
        builder.Services.TryAddSingleton<AuditableEntityInterceptor>();
        builder.Services.AddHttpContextAccessor();

        builder.AddModuleNpgsqlDbContext<UsersDbContext>(
            "cartrack",
            MigrationsHistoryTable,
            (sp, options) =>
            {
                var interceptor = sp.GetRequiredService<AuditableEntityInterceptor>();
                options.AddInterceptors(interceptor);
            });

        builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection(JwtOptions.SectionName));
        builder.Services.Configure<SeedAdminOptions>(builder.Configuration.GetSection(SeedAdminOptions.SectionName));
        builder.Services.Configure<AppUrlOptions>(builder.Configuration.GetSection(AppUrlOptions.SectionName));
        builder.Services.Configure<EmailOptions>(builder.Configuration.GetSection(EmailOptions.SectionName));

        builder.Services.AddScoped<ITokenService, TokenService>();
        builder.Services.AddScoped<IOrgDirectory, OrgDirectory>();
        builder.Services.AddScoped<ICurrentUserScope, CurrentUserScope>();
        builder.Services.AddScoped<IPermissionService, PermissionService>();
        builder.Services.AddScoped<IUserService, UserService>();
        builder.Services.AddScoped<ISecurityAuditService, SecurityAuditService>();
        builder.Services.AddScoped<IAccountEmailService, AccountEmailService>();
        builder.Services.AddSingleton<IAuthorizationHandler, PermissionAuthorizationHandler>();
        builder.Services.AddSingleton<IAuthorizationHandler, AnyPermissionAuthorizationHandler>();

        var emailOptions = builder.Configuration.GetSection(EmailOptions.SectionName).Get<EmailOptions>() ?? new EmailOptions();
        if (emailOptions.Enabled && !string.IsNullOrWhiteSpace(emailOptions.SmtpHost))
        {
            builder.Services.AddSingleton<IEmailService, SmtpEmailService>();
        }
        else
        {
            builder.Services.AddSingleton<IEmailService, LoggingEmailService>();
        }
    }

    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGroup("/api/auth").MapAuthEndpoints();
        endpoints.MapGroup("/api/users").MapUserEndpoints();
        endpoints.MapGroup("/api/roles").MapRoleEndpoints();
        endpoints.MapGroup("/api/permissions").MapPermissionEndpoints();
    }

    public async Task MigrateAsync(IServiceProvider services, CancellationToken cancellationToken = default)
    {
        await using var scope = services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<UsersDbContext>();
        await db.MigrateModuleAsync(MigrationsHistoryTable, ProbeTable, cancellationToken);
    }

    public Task SeedAsync(IServiceProvider services, CancellationToken cancellationToken = default) =>
        UsersSeeder.SeedAsync(services, cancellationToken);
}
