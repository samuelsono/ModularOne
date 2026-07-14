using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using CarTrack.Server.Auth;
using CarTrack.Server.Auth.Email;
using CarTrack.Server.CarTrack;
using CarTrack.Server.Configuration;
using CarTrack.Server.Data;
using CarTrack.Server.Drivers;
using CarTrack.Server.CoreHr;
using CarTrack.Server.Expense;
using CarTrack.Server.Help;
using CarTrack.Server.Leave;
using CarTrack.Server.Notifications;
using CarTrack.Server.Reports;
using CarTrack.Server.Settings;
using CarTrack.Server.Support;
using CarTrack.Server.Users;
using CarTrack.Server.Users.Authorization;
using CarTrack.Server.Vehicles;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;

EnvFileConfiguration.LoadEnvFile();

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.AddRedisClientBuilder("cache")
    .WithOutputCache();
builder.AddNpgsqlDbContext<ApplicationDbContext>("cartrack");
builder.Services.AddSingleton<AuditableEntityInterceptor>();
builder.Services.AddSingleton<IDbContextOptionsConfiguration<ApplicationDbContext>, AuditableDbContextOptionsConfiguration>();

builder.Services.AddProblemDetails();
builder.Services.AddOpenApi();
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
});

builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection(JwtOptions.SectionName));
builder.Services.Configure<SeedAdminOptions>(builder.Configuration.GetSection(SeedAdminOptions.SectionName));
builder.Services.Configure<CarTrackOptions>(builder.Configuration.GetSection(CarTrackOptions.SectionName));
builder.Services.Configure<AppUrlOptions>(builder.Configuration.GetSection(AppUrlOptions.SectionName));
builder.Services.Configure<EmailOptions>(builder.Configuration.GetSection(EmailOptions.SectionName));
builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUserScope, CurrentUserScope>();
builder.Services.AddScoped<IPermissionService, PermissionService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<ISecurityAuditService, SecurityAuditService>();
builder.Services.AddScoped<IAccountEmailService, AccountEmailService>();
builder.Services.AddScoped<ILeaveApprovalService, LeaveApprovalService>();
builder.Services.AddScoped<ILeaveConfigurationService, LeaveConfigurationService>();
builder.Services.AddScoped<ILeaveWorkingDaysService, LeaveWorkingDaysService>();
builder.Services.AddScoped<ILeaveBalanceService, LeaveBalanceService>();
builder.Services.AddScoped<ILeaveCalendarService, LeaveCalendarService>();
builder.Services.AddScoped<ILeaveReportService, LeaveReportService>();
builder.Services.AddScoped<ILeaveDocumentStorage, LeaveDocumentStorage>();
builder.Services.AddScoped<ILeaveAccrualService, LeaveAccrualService>();
builder.Services.AddScoped<IPublicHolidaySyncService, PublicHolidaySyncService>();
builder.Services.AddHttpClient<IOpenHolidaysApiClient, OpenHolidaysApiClient>(client =>
{
    client.BaseAddress = new Uri("https://openholidaysapi.org/");
});
builder.Services.AddScoped<IExpenseApprovalService, ExpenseApprovalService>();
builder.Services.AddScoped<IExpenseCategoryService, ExpenseCategoryService>();
builder.Services.AddScoped<IExpenseReportService, ExpenseReportService>();
builder.Services.AddScoped<ICoreHrService, CoreHrService>();
builder.Services.AddSingleton<IAuthorizationHandler, PermissionAuthorizationHandler>();
builder.Services.AddSingleton<IAuthorizationHandler, AnyPermissionAuthorizationHandler>();
builder.Services.AddScoped<IVehicleService, VehicleService>();
builder.Services.AddScoped<IDriverService, DriverService>();
builder.Services.AddScoped<ICarTrackCredentialProvider, CarTrackCredentialProvider>();
builder.Services.AddScoped<ICarTrackSettingsService, CarTrackSettingsService>();
builder.Services.AddScoped<IPlatformSettingsService, PlatformSettingsService>();
builder.Services.AddScoped<IDashboardService, DashboardService>();
builder.Services.AddScoped<IReportService, ReportService>();
builder.Services.AddScoped<IApiTableReportExecutor, ApiTableReportExecutor>();
builder.Services.AddScoped<IReportQueryService, ReportQueryService>();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<ISupportService, SupportService>();
builder.Services.AddScoped<IHelpService, HelpService>();
builder.Services.AddHostedService<NotificationCleanupService>();
builder.Services.AddHostedService<LeaveAccrualBackgroundService>();
builder.Services.AddSignalR();
builder.Services.AddDataProtection();
builder.Services.AddHttpClient(nameof(CarTrackSettingsService));

var emailOptions = builder.Configuration.GetSection(EmailOptions.SectionName).Get<EmailOptions>() ?? new EmailOptions();
if (emailOptions.Enabled && !string.IsNullOrWhiteSpace(emailOptions.SmtpHost))
{
    builder.Services.AddSingleton<IEmailService, SmtpEmailService>();
}
else
{
    builder.Services.AddSingleton<IEmailService, LoggingEmailService>();
}

builder.Services.AddHttpClient<ICarTrackApiClient, CarTrackApiClient>((sp, client) =>
{
    var options = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<CarTrackOptions>>().Value;
    client.BaseAddress = new Uri(options.BaseUrl.TrimEnd('/') + "/");
    // Let the Polly resilience pipeline own the timeout budget; HttpClient's own
    // timeout would otherwise cancel long-running requests before Polly can.
    client.Timeout = Timeout.InfiniteTimeSpan;
});

var jwtOptions = builder.Configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>()
    ?? throw new InvalidOperationException("JWT configuration is missing.");

if (string.IsNullOrWhiteSpace(jwtOptions.Secret) || jwtOptions.Secret.Length < 32)
{
    throw new InvalidOperationException("JWT secret must be at least 32 characters.");
}

builder.Services
    .AddIdentityCore<ApplicationUser>(options =>
    {
        options.User.RequireUniqueEmail = true;
        options.Password.RequiredLength = 8;
        options.Password.RequireDigit = true;
        options.Password.RequireLowercase = true;
        options.Password.RequireUppercase = true;
        options.Password.RequireNonAlphanumeric = true;
        options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
        options.Lockout.MaxFailedAccessAttempts = 5;
        options.Lockout.AllowedForNewUsers = true;
    })
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddSignInManager()
    .AddDefaultTokenProviders();

builder.Services.Configure<DataProtectionTokenProviderOptions>(options =>
{
    options.TokenLifespan = TimeSpan.FromHours(72);
});

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtOptions.Issuer,
            ValidAudience = jwtOptions.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.Secret)),
            ClockSkew = TimeSpan.FromMinutes(1),
            NameClaimType = JwtRegisteredClaimNames.Sub,
        };

        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var accessToken = context.Request.Query["access_token"];
                var path = context.HttpContext.Request.Path;
                if (!string.IsNullOrEmpty(accessToken)
                    && path.StartsWithSegments("/hubs/notifications"))
                {
                    context.Token = accessToken;
                }

                return Task.CompletedTask;
            },
        };
    });

builder.Services.AddAuthorization();

var app = builder.Build();

app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseOutputCache();
app.UseAuthentication();
app.UseAuthorization();

var api = app.MapGroup("/api");
api.MapGroup("/auth").MapAuthEndpoints();
api.MapGroup("/users").MapUserEndpoints();
api.MapGroup("/roles").MapRoleEndpoints();
api.MapGroup("/permissions").MapPermissionEndpoints();
api.MapGroup("/leave").MapLeaveEndpoints();
api.MapGroup("/expense").MapExpenseEndpoints();
api.MapGroup("/core").MapCoreHrEndpoints();
api.MapGroup("/settings").MapSettingsEndpoints();
api.MapGroup("/vehicles").MapVehicleEndpoints();
api.MapGroup("/drivers").MapDriverEndpoints();
api.MapGroup("/dashboards").MapDashboardEndpoints();
api.MapGroup("/reports").MapReportEndpoints();
api.MapGroup("/notifications").MapNotificationEndpoints();
api.MapGroup("/support").MapSupportEndpoints();
api.MapGroup("/help").MapHelpEndpoints();

app.MapHub<NotificationHub>("/hubs/notifications");

string[] summaries = ["Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"];

api.MapGet("weatherforecast", () =>
{
    var forecast = Enumerable.Range(1, 5).Select(index =>
        new WeatherForecast
        (
            DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
            Random.Shared.Next(-20, 55),
            summaries[Random.Shared.Next(summaries.Length)]
        ))
        .ToArray();
    return forecast;
})
.CacheOutput(p => p.Expire(TimeSpan.FromSeconds(5)))
.WithName("GetWeatherForecast")
.RequireAuthorization();

app.MapDefaultEndpoints();

app.UseFileServer();

await DatabaseSeeder.SeedAsync(app.Services);

app.Run();

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}
