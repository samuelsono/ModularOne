using System.Security.Claims;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using CarTrack.Api;
using CarTrack.Host;
using CarTrack.Modules.CoreHr;
using CarTrack.Modules.Expense;
using CarTrack.Modules.Fleet;
using CarTrack.Modules.Help;
using CarTrack.Modules.Leave;
using CarTrack.Modules.Notifications;
using CarTrack.Modules.Reporting;
using CarTrack.Modules.Settings;
using CarTrack.Modules.Support;
using CarTrack.Modules.Tenders;
using CarTrack.Modules.Users;
using CarTrack.Server.Auth;
using CarTrack.Server.Configuration;
using CarTrack.Server.Data;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;

EnvFileConfiguration.LoadEnvFile();

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.AddRedisClientBuilder("cache")
    .WithOutputCache();
builder.AddNpgsqlDbContext<ApplicationDbContext>("cartrack");

// Persist DataProtection keys with a stable app name. Default discriminator follows
// ContentRoot, so renaming CarTrack.Server → CarTrack.Host orphaned encrypted
// CarTrackSettings passwords and made Fleet API calls report "not configured".
var dataProtectionKeysPath = Path.Combine(builder.Environment.ContentRootPath, "App_Data", "DataProtection-Keys");
Directory.CreateDirectory(dataProtectionKeysPath);
ImportLegacyDataProtectionKeys(dataProtectionKeysPath);
builder.Services.AddDataProtection()
    .SetApplicationName("CarTrack")
    .PersistKeysToFileSystem(new DirectoryInfo(dataProtectionKeysPath));

builder.Services.AddProblemDetails();
builder.Services.AddOpenApi();
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
});

var modules = new ModuleRegistry()
    .Add(new HelpModule())
    .Add(new SupportModule())
    .Add(new CoreHrModule())
    .Add(new NotificationsModule())
    .Add(new LeaveModule())
    .Add(new ExpenseModule())
    .Add(new FleetModule())
    .Add(new UsersModule())
    .Add(new SettingsModule())
    .Add(new ReportingModule())
    .Add(new TendersModule())
    .Add(new HostModule());

modules.AddModules(builder);

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
            // Tokens emit ClaimTypes.Role which JwtSecurityTokenHandler may shorten to "role".
            RoleClaimType = ClaimTypes.Role,
        };

        // Keep claim types as written so ClaimTypes.Role / "role" lookups stay reliable.
        options.MapInboundClaims = false;

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

modules.MapEndpoints(app);

app.MapDefaultEndpoints();
app.UseFileServer();

await modules.MigrateAllAsync(app.Services);
await modules.SeedAllAsync(app.Services);

app.Run();

static void ImportLegacyDataProtectionKeys(string destinationPath)
{
    var legacyPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
        ".aspnet",
        "DataProtection-Keys");

    if (!Directory.Exists(legacyPath))
    {
        return;
    }

    foreach (var sourceFile in Directory.EnumerateFiles(legacyPath, "key-*.xml"))
    {
        var destinationFile = Path.Combine(destinationPath, Path.GetFileName(sourceFile));
        if (!File.Exists(destinationFile))
        {
            File.Copy(sourceFile, destinationFile);
        }
    }
}

