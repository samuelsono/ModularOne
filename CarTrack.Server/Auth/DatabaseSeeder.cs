using CarTrack.Server.CoreHr;
using CarTrack.Server.Data;
using CarTrack.Server.Expense;
using CarTrack.Server.Leave;
using CarTrack.Server.Reports;
using CarTrack.Server.Support;
using CarTrack.Server.Users;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace CarTrack.Server.Auth;

public class SeedAdminOptions
{
    public const string SectionName = "SeedAdmin";

    public string Username { get; set; } = "admin";

    public string Email { get; set; } = "admin@cartrack.local";

    public string Password { get; set; } = "Admin123!";

    public string DisplayName { get; set; } = "System Administrator";
}

public static class DatabaseSeeder
{
    public const string AdminRole = "Admin";

    public static async Task SeedAsync(IServiceProvider services, CancellationToken cancellationToken = default)
    {
        using var scope = services.CreateScope();
        var scopedServices = scope.ServiceProvider;

        var dbContext = scopedServices.GetRequiredService<ApplicationDbContext>();
        await dbContext.Database.MigrateAsync(cancellationToken);

        var roleManager = scopedServices.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = scopedServices.GetRequiredService<UserManager<ApplicationUser>>();
        var seedOptions = scopedServices.GetRequiredService<IOptions<SeedAdminOptions>>().Value;

        await PermissionSeeder.SeedAsync(dbContext, roleManager, cancellationToken);

        var adminUser = await userManager.FindByNameAsync(seedOptions.Username);
        if (adminUser is null)
        {
            adminUser = new ApplicationUser
            {
                UserName = seedOptions.Username,
                Email = seedOptions.Email,
                EmailConfirmed = true,
                DisplayName = seedOptions.DisplayName,
                IsActive = true,
                CreatedAt = DateTimeOffset.UtcNow,
            };

            var createResult = await userManager.CreateAsync(adminUser, seedOptions.Password);
            if (!createResult.Succeeded)
            {
                var errors = string.Join(", ", createResult.Errors.Select(error => error.Description));
                throw new InvalidOperationException($"Failed to seed admin user: {errors}");
            }
        }
        else if (adminUser.CreatedAt == default)
        {
            adminUser.CreatedAt = DateTimeOffset.UtcNow;
            adminUser.IsActive = true;
            await userManager.UpdateAsync(adminUser);
        }

        if (!await userManager.IsInRoleAsync(adminUser, AppRoles.SystemAdmin))
        {
            await userManager.AddToRoleAsync(adminUser, AppRoles.SystemAdmin);
        }

        if (!await userManager.IsInRoleAsync(adminUser, AdminRole))
        {
            await userManager.AddToRoleAsync(adminUser, AdminRole);
        }

        await DashboardSeeder.SeedAsync(dbContext, cancellationToken);
        await SupportTicketSeeder.SeedAsync(dbContext, cancellationToken);
        await LeaveSeeder.SeedAsync(dbContext, cancellationToken);
        var leaveWorkingDaysService = scopedServices.GetRequiredService<ILeaveWorkingDaysService>();
        await LeaveSeeder.BackfillWorkingDaysAsync(dbContext, leaveWorkingDaysService, cancellationToken);
        await ExpenseSeeder.SeedAsync(dbContext, cancellationToken);
        await CoreHrSeeder.SeedAsync(dbContext, cancellationToken);
    }
}
