using System.Security.Claims;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace CarTrack.Server.Data;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    private readonly IHttpContextAccessor? _httpContextAccessor;

    public ApplicationDbContext(
        DbContextOptions<ApplicationDbContext> options,
        IHttpContextAccessor? httpContextAccessor = null)
        : base(options)
    {
        _httpContextAccessor = httpContextAccessor;
    }
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    public DbSet<Vehicle> Vehicles => Set<Vehicle>();

    public DbSet<CarTrackPageCache> CarTrackPageCache => Set<CarTrackPageCache>();

    public DbSet<Driver> Drivers => Set<Driver>();

    public DbSet<CarTrackSettings> CarTrackSettings => Set<CarTrackSettings>();

    public DbSet<PlatformSettings> PlatformSettings => Set<PlatformSettings>();

    public DbSet<Dashboard> Dashboards => Set<Dashboard>();

    public DbSet<DashboardSection> DashboardSections => Set<DashboardSection>();

    public DbSet<ReportDefinition> ReportDefinitions => Set<ReportDefinition>();

    public DbSet<ReportPlacement> ReportPlacements => Set<ReportPlacement>();

    public DbSet<StaffProfile> StaffProfiles => Set<StaffProfile>();

    public DbSet<DriverProfileLink> DriverProfileLinks => Set<DriverProfileLink>();

    public DbSet<Permission> Permissions => Set<Permission>();

    public DbSet<RolePermission> RolePermissions => Set<RolePermission>();

    public DbSet<LeaveType> LeaveTypes => Set<LeaveType>();

    public DbSet<PublicHoliday> PublicHolidays => Set<PublicHoliday>();

    public DbSet<LeaveBalance> LeaveBalances => Set<LeaveBalance>();

    public DbSet<LeaveRequest> LeaveRequests => Set<LeaveRequest>();

    public DbSet<ExpenseClaim> ExpenseClaims => Set<ExpenseClaim>();

    public DbSet<ExpenseCategory> ExpenseCategories => Set<ExpenseCategory>();

    public DbSet<Company> Companies => Set<Company>();

    public DbSet<Department> Departments => Set<Department>();

    public DbSet<Position> Positions => Set<Position>();

    public DbSet<SecurityAuditLog> SecurityAuditLogs => Set<SecurityAuditLog>();

    public DbSet<Notification> Notifications => Set<Notification>();

    public DbSet<NotificationRecipient> NotificationRecipients => Set<NotificationRecipient>();

    public DbSet<SupportTicket> SupportTickets => Set<SupportTicket>();

    public DbSet<TicketCategory> TicketCategories => Set<TicketCategory>();

    public DbSet<HelpArticle> HelpArticles => Set<HelpArticle>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<RefreshToken>(entity =>
        {
            entity.HasKey(token => token.Id);
            entity.HasIndex(token => token.TokenHash).IsUnique();
            entity.HasIndex(token => new { token.UserId, token.ExpiresAt });

            entity.Property(token => token.TokenHash)
                .HasMaxLength(128)
                .IsRequired();

            entity.HasOne(token => token.User)
                .WithMany()
                .HasForeignKey(token => token.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<Vehicle>(entity =>
        {
            entity.HasKey(vehicle => vehicle.Id);
            entity.HasIndex(vehicle => vehicle.CarTrackVehicleId).IsUnique();
            entity.HasIndex(vehicle => vehicle.RegistrationNumber)
                .IsUnique()
                .HasFilter("\"IsDeleted\" = false");

            entity.Property(vehicle => vehicle.RegistrationNumber)
                .HasMaxLength(32)
                .IsRequired();

            entity.Property(vehicle => vehicle.Make).HasMaxLength(128);
            entity.Property(vehicle => vehicle.Model).HasMaxLength(128);
            entity.Property(vehicle => vehicle.Vin).HasMaxLength(64);
            entity.Property(vehicle => vehicle.EngineNumber).HasMaxLength(64);
            entity.Property(vehicle => vehicle.Colour).HasMaxLength(64);
            entity.Property(vehicle => vehicle.VehicleType).HasMaxLength(128);
            entity.Property(vehicle => vehicle.FuelType).HasMaxLength(32);
            entity.Property(vehicle => vehicle.RegisteredOwner).HasMaxLength(256);
            entity.Property(vehicle => vehicle.IgnitionStatus).HasMaxLength(16);
            entity.Property(vehicle => vehicle.EngineType).HasMaxLength(64);
            entity.Property(vehicle => vehicle.PositionDescription).HasMaxLength(512);
            entity.Property(vehicle => vehicle.ElectricChargingStatus).HasMaxLength(64);
            entity.Property(vehicle => vehicle.DriverId).HasMaxLength(64);
            entity.Property(vehicle => vehicle.DriverFirstName).HasMaxLength(128);
            entity.Property(vehicle => vehicle.DriverLastName).HasMaxLength(128);
            entity.Property(vehicle => vehicle.DriverPhone).HasMaxLength(32);
            entity.Property(vehicle => vehicle.DriverLicenseNumber).HasMaxLength(64);
            entity.Property(vehicle => vehicle.GeofenceIdsJson).HasMaxLength(2048);
            entity.HasIndex(vehicle => vehicle.AssignedDriverId);

            entity.HasOne(vehicle => vehicle.AssignedDriver)
                .WithMany()
                .HasForeignKey(vehicle => vehicle.AssignedDriverId)
                .OnDelete(DeleteBehavior.SetNull);

            ConfigureAuditable(entity);
        });

        builder.Entity<CarTrackPageCache>(entity =>
        {
            entity.HasKey(cache => cache.Id);
            entity.HasIndex(cache => new
            {
                cache.Resource,
                cache.Registration,
                cache.RangeKey,
                cache.Page,
                cache.PerPage,
            }).IsUnique();

            entity.Property(cache => cache.Resource).HasMaxLength(16).IsRequired();
            entity.Property(cache => cache.Registration).HasMaxLength(32).IsRequired();
            entity.Property(cache => cache.RangeKey).HasMaxLength(64);
            entity.Property(cache => cache.PayloadJson).IsRequired();
        });

        builder.Entity<Driver>(entity =>
        {
            entity.HasKey(driver => driver.Id);
            entity.HasIndex(driver => driver.DriverCode).IsUnique();
            entity.HasIndex(driver => driver.WorkEmail).IsUnique();
            entity.HasIndex(driver => driver.LicenceNumber).IsUnique();

            entity.Property(driver => driver.DriverCode).HasMaxLength(16).IsRequired();
            entity.Property(driver => driver.FirstName).HasMaxLength(128).IsRequired();
            entity.Property(driver => driver.LastName).HasMaxLength(128).IsRequired();
            entity.Property(driver => driver.WorkEmail).HasMaxLength(256).IsRequired();
            entity.Property(driver => driver.WorkPhone).HasMaxLength(32).IsRequired();
            entity.Property(driver => driver.WhatsappNumber).HasMaxLength(32);
            entity.Property(driver => driver.Gender).HasMaxLength(16);
            entity.Property(driver => driver.LicenceNumber).HasMaxLength(64).IsRequired();
            entity.Property(driver => driver.IdNumber).HasMaxLength(32).IsRequired();
            entity.Property(driver => driver.LicenceCode).HasMaxLength(8);
            entity.Property(driver => driver.UnitName).HasMaxLength(128);
            entity.Property(driver => driver.StreetNumber).HasMaxLength(16);
            entity.Property(driver => driver.StreetName).HasMaxLength(128);
            entity.Property(driver => driver.Suburb).HasMaxLength(128);
            entity.Property(driver => driver.City).HasMaxLength(128);
            entity.Property(driver => driver.Province).HasMaxLength(64);
            entity.Property(driver => driver.PostalCode).HasMaxLength(16);
            entity.Property(driver => driver.EmployeeNumber).HasMaxLength(64);
            entity.Property(driver => driver.JobTitle).HasMaxLength(128);
            entity.Property(driver => driver.Department).HasMaxLength(128);
            entity.Property(driver => driver.Branch).HasMaxLength(128);
            entity.Property(driver => driver.EmploymentType).HasMaxLength(32);
            entity.Property(driver => driver.EmploymentStatus).HasMaxLength(32);
            entity.Property(driver => driver.Manager).HasMaxLength(128);
            entity.Property(driver => driver.CarTrackDriverId).HasMaxLength(64);
            entity.HasIndex(driver => driver.CarTrackDriverId);
            ConfigureAuditable(entity);
        });

        builder.Entity<Notification>(entity =>
        {
            entity.HasKey(notification => notification.Id);
            entity.Property(notification => notification.Title).HasMaxLength(120).IsRequired();
            entity.Property(notification => notification.Body).HasMaxLength(1000).IsRequired();
            entity.Property(notification => notification.RelatedEntityId).HasMaxLength(128);
            entity.Property(notification => notification.TargetUserId).HasMaxLength(450);
            entity.Property(notification => notification.TargetGroupName).HasMaxLength(128);
            entity.Property(notification => notification.CreatedByUserId).HasMaxLength(450);

            entity.HasOne(notification => notification.CreatedByUser)
                .WithMany()
                .HasForeignKey(notification => notification.CreatedByUserId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        builder.Entity<NotificationRecipient>(entity =>
        {
            entity.HasKey(recipient => recipient.Id);
            entity.HasIndex(recipient => new { recipient.UserId, recipient.IsRead, recipient.IsArchived });
            entity.HasIndex(recipient => recipient.ReadAt);
            entity.Property(recipient => recipient.UserId).HasMaxLength(450).IsRequired();

            entity.HasOne(recipient => recipient.Notification)
                .WithMany(notification => notification.Recipients)
                .HasForeignKey(recipient => recipient.NotificationId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(recipient => recipient.User)
                .WithMany()
                .HasForeignKey(recipient => recipient.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<CarTrackSettings>(entity =>
        {
            entity.HasKey(settings => settings.Id);
            entity.Property(settings => settings.Id).ValueGeneratedNever();
            entity.Property(settings => settings.BaseUrl).HasMaxLength(512).IsRequired();
            entity.Property(settings => settings.Username).HasMaxLength(256).IsRequired();
            entity.Property(settings => settings.ProtectedPassword).HasMaxLength(2048);
        });

        builder.Entity<PlatformSettings>(entity =>
        {
            entity.HasKey(settings => settings.Id);
            entity.Property(settings => settings.Id).ValueGeneratedNever();
            entity.Property(settings => settings.DefaultModuleSlugValue)
                .HasColumnName("DefaultModuleSlug")
                .HasMaxLength(64)
                .IsRequired();
        });

        builder.Entity<Dashboard>(entity =>
        {
            entity.HasKey(dashboard => dashboard.Id);
            entity.HasIndex(dashboard => dashboard.IsDefault);
            entity.Property(dashboard => dashboard.Name).HasMaxLength(256).IsRequired();
            entity.Property(dashboard => dashboard.Description).HasMaxLength(1024);
        });

        builder.Entity<DashboardSection>(entity =>
        {
            entity.HasKey(section => section.Id);
            entity.HasIndex(section => new { section.DashboardId, section.SortOrder });
            entity.HasIndex(section => section.ParentSectionId);
            entity.Property(section => section.Title).HasMaxLength(256);
            entity.Property(section => section.Subtitle).HasMaxLength(512);
            entity.Property(section => section.LayoutDirection).HasMaxLength(16).IsRequired();
            entity.Property(section => section.Size).HasMaxLength(32).IsRequired();

            entity.HasOne(section => section.Dashboard)
                .WithMany(dashboard => dashboard.Sections)
                .HasForeignKey(section => section.DashboardId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(section => section.ParentSection)
                .WithMany(section => section.ChildSections)
                .HasForeignKey(section => section.ParentSectionId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<ReportDefinition>(entity =>
        {
            entity.HasKey(report => report.Id);
            entity.Property(report => report.Name).HasMaxLength(256).IsRequired();
            entity.Property(report => report.Description).HasMaxLength(1024);
            entity.Property(report => report.ReportType).HasMaxLength(32).IsRequired();
            entity.Property(report => report.Size).HasMaxLength(32).IsRequired();
            entity.Property(report => report.TargetTable).HasMaxLength(64).IsRequired();
            entity.Property(report => report.AggregateFunction).HasMaxLength(32).IsRequired();
            entity.Property(report => report.AggregateField).HasMaxLength(128);
            entity.Property(report => report.GroupByColumnsJson).IsRequired();
            entity.Property(report => report.FiltersJson).IsRequired();
        });

        builder.Entity<ReportPlacement>(entity =>
        {
            entity.HasKey(placement => placement.Id);
            entity.HasIndex(placement => new { placement.SectionId, placement.SortOrder });
            entity.HasIndex(placement => new { placement.ReportId, placement.SectionId }).IsUnique();
            entity.Property(placement => placement.Size).HasMaxLength(32).IsRequired();

            entity.HasOne(placement => placement.Report)
                .WithMany(report => report.Placements)
                .HasForeignKey(placement => placement.ReportId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(placement => placement.Section)
                .WithMany(section => section.ReportPlacements)
                .HasForeignKey(placement => placement.SectionId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<ApplicationUser>(entity =>
        {
            entity.Property(user => user.FirstName).HasMaxLength(128);
            entity.Property(user => user.LastName).HasMaxLength(128);
            entity.HasIndex(user => user.IsActive);
        });

        builder.Entity<StaffProfile>(entity =>
        {
            entity.HasKey(profile => profile.Id);
            entity.HasIndex(profile => profile.UserId).IsUnique();
            entity.HasIndex(profile => profile.ManagerUserId);

            entity.Property(profile => profile.EmployeeNumber).HasMaxLength(64);
            entity.Property(profile => profile.JobTitle).HasMaxLength(128);
            entity.Property(profile => profile.Department).HasMaxLength(128);
            entity.Property(profile => profile.Branch).HasMaxLength(128);
            entity.Property(profile => profile.EmploymentStatus).HasMaxLength(32).IsRequired();

            entity.HasOne(profile => profile.User)
                .WithOne(user => user.StaffProfile)
                .HasForeignKey<StaffProfile>(profile => profile.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(profile => profile.Manager)
                .WithMany()
                .HasForeignKey(profile => profile.ManagerUserId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(profile => profile.Company)
                .WithMany(company => company.StaffProfiles)
                .HasForeignKey(profile => profile.CompanyId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(profile => profile.AssignedDepartment)
                .WithMany(department => department.StaffProfiles)
                .HasForeignKey(profile => profile.DepartmentId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(profile => profile.AssignedPosition)
                .WithMany(position => position.StaffProfiles)
                .HasForeignKey(profile => profile.PositionId)
                .OnDelete(DeleteBehavior.SetNull);

            ConfigureAuditable(entity);
        });

        builder.Entity<Company>(entity =>
        {
            entity.HasKey(company => company.Id);
            entity.HasIndex(company => company.Code).IsUnique();
            entity.Property(company => company.Name).HasMaxLength(128).IsRequired();
            entity.Property(company => company.Code).HasMaxLength(32).IsRequired();
            entity.Property(company => company.Description).HasMaxLength(512);
            ConfigureAuditable(entity);
        });

        builder.Entity<Department>(entity =>
        {
            entity.HasKey(department => department.Id);
            entity.HasIndex(department => new { department.CompanyId, department.Code }).IsUnique();
            entity.Property(department => department.Name).HasMaxLength(128).IsRequired();
            entity.Property(department => department.Code).HasMaxLength(32).IsRequired();
            entity.Property(department => department.Description).HasMaxLength(512);

            entity.HasOne(department => department.Company)
                .WithMany(company => company.Departments)
                .HasForeignKey(department => department.CompanyId)
                .OnDelete(DeleteBehavior.Restrict);

            ConfigureAuditable(entity);
        });

        builder.Entity<Position>(entity =>
        {
            entity.HasKey(position => position.Id);
            entity.HasIndex(position => new { position.DepartmentId, position.Code }).IsUnique();
            entity.Property(position => position.Name).HasMaxLength(128).IsRequired();
            entity.Property(position => position.Code).HasMaxLength(32).IsRequired();
            entity.Property(position => position.Description).HasMaxLength(512);

            entity.HasOne(position => position.Department)
                .WithMany(department => department.Positions)
                .HasForeignKey(position => position.DepartmentId)
                .OnDelete(DeleteBehavior.Restrict);

            ConfigureAuditable(entity);
        });

        builder.Entity<DriverProfileLink>(entity =>
        {
            entity.HasKey(link => link.Id);
            entity.HasIndex(link => link.UserId).IsUnique();
            entity.HasIndex(link => link.DriverId).IsUnique();

            entity.HasOne(link => link.User)
                .WithOne(user => user.DriverLink)
                .HasForeignKey<DriverProfileLink>(link => link.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(link => link.Driver)
                .WithMany()
                .HasForeignKey(link => link.DriverId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<Permission>(entity =>
        {
            entity.HasKey(permission => permission.Id);
            entity.HasIndex(permission => permission.Key).IsUnique();

            entity.Property(permission => permission.Key).HasMaxLength(128).IsRequired();
            entity.Property(permission => permission.ModuleSlug).HasMaxLength(64).IsRequired();
            entity.Property(permission => permission.SubmoduleSlug).HasMaxLength(64).IsRequired();
            entity.Property(permission => permission.Action).HasMaxLength(16).IsRequired();
            entity.Property(permission => permission.Description).HasMaxLength(512);
        });

        builder.Entity<RolePermission>(entity =>
        {
            entity.HasKey(mapping => mapping.Id);
            entity.HasIndex(mapping => new { mapping.RoleId, mapping.PermissionId }).IsUnique();

            entity.HasOne(mapping => mapping.Permission)
                .WithMany(permission => permission.RolePermissions)
                .HasForeignKey(mapping => mapping.PermissionId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<LeaveType>(entity =>
        {
            entity.HasKey(type => type.Id);
            entity.HasIndex(type => type.Code).IsUnique();

            entity.Property(type => type.Name).HasMaxLength(128).IsRequired();
            entity.Property(type => type.Code).HasMaxLength(32).IsRequired();
            entity.Property(type => type.Color).HasMaxLength(16).IsRequired();
            entity.Property(type => type.AccrualMethod).HasMaxLength(16).IsRequired();
            entity.Property(type => type.AnnualEntitlement).HasPrecision(6, 2);
            ConfigureAuditable(entity);
        });

        builder.Entity<PublicHoliday>(entity =>
        {
            entity.HasKey(holiday => holiday.Id);
            entity.HasIndex(holiday => holiday.Date);

            entity.Property(holiday => holiday.Name).HasMaxLength(128).IsRequired();
            entity.Property(holiday => holiday.Branch).HasMaxLength(128);
            entity.Property(holiday => holiday.ExternalId).HasMaxLength(64);
            entity.HasIndex(holiday => holiday.ExternalId);
        });

        builder.Entity<LeaveBalance>(entity =>
        {
            entity.HasKey(balance => balance.Id);
            entity.HasIndex(balance => new { balance.UserId, balance.LeaveTypeId, balance.CycleStart, balance.CycleEnd }).IsUnique();

            entity.HasOne(balance => balance.User)
                .WithMany()
                .HasForeignKey(balance => balance.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(balance => balance.LeaveType)
                .WithMany()
                .HasForeignKey(balance => balance.LeaveTypeId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<LeaveRequest>(entity =>
        {
            entity.HasKey(request => request.Id);
            entity.HasIndex(request => new { request.ManagerUserId, request.Status });
            entity.HasIndex(request => request.RequesterUserId);

            entity.Property(request => request.Status).HasMaxLength(16).IsRequired();
            entity.Property(request => request.Notes).HasMaxLength(1024);
            entity.Property(request => request.RequesterBranch).HasMaxLength(128);
            entity.Property(request => request.DocumentPath).HasMaxLength(512);
            entity.Property(request => request.DocumentFileName).HasMaxLength(256);
            entity.Property(request => request.DocumentContentType).HasMaxLength(128);
            entity.Property(request => request.WorkingDays).HasPrecision(6, 2);
            entity.Property(request => request.StartDayPortion).HasMaxLength(8).IsRequired();
            entity.Property(request => request.EndDayPortion).HasMaxLength(8).IsRequired();

            entity.HasOne(request => request.Requester)
                .WithMany()
                .HasForeignKey(request => request.RequesterUserId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(request => request.Manager)
                .WithMany()
                .HasForeignKey(request => request.ManagerUserId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(request => request.LeaveType)
                .WithMany(type => type.Requests)
                .HasForeignKey(request => request.LeaveTypeId)
                .OnDelete(DeleteBehavior.Restrict);

            ConfigureAuditable(entity);
        });

        builder.Entity<ExpenseCategory>(entity =>
        {
            entity.HasKey(category => category.Id);
            entity.HasIndex(category => category.Code).IsUnique();
            entity.Property(category => category.Name).HasMaxLength(128).IsRequired();
            entity.Property(category => category.Code).HasMaxLength(32).IsRequired();
            entity.Property(category => category.Description).HasMaxLength(512);
            ConfigureAuditable(entity);
        });

        builder.Entity<ExpenseClaim>(entity =>
        {
            entity.HasKey(claim => claim.Id);
            entity.HasIndex(claim => new { claim.ManagerUserId, claim.Status });
            entity.HasIndex(claim => claim.RequesterUserId);
            entity.HasIndex(claim => new { claim.RequesterUserId, claim.ExpenseDate });

            entity.Property(claim => claim.Description).HasMaxLength(512).IsRequired();
            entity.Property(claim => claim.Notes).HasMaxLength(1024);
            entity.Property(claim => claim.Currency).HasMaxLength(8).IsRequired();
            entity.Property(claim => claim.Status).HasMaxLength(16).IsRequired();
            entity.Property(claim => claim.RequesterBranch).HasMaxLength(128);
            entity.Property(claim => claim.Amount).HasPrecision(18, 2);
            entity.Property(claim => claim.PaidByUserId).HasMaxLength(450);

            entity.HasOne(claim => claim.Requester)
                .WithMany()
                .HasForeignKey(claim => claim.RequesterUserId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(claim => claim.Manager)
                .WithMany()
                .HasForeignKey(claim => claim.ManagerUserId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(claim => claim.Category)
                .WithMany(category => category.Claims)
                .HasForeignKey(claim => claim.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);

            ConfigureAuditable(entity);
        });

        builder.Entity<SecurityAuditLog>(entity =>
        {
            entity.HasKey(entry => entry.Id);
            entity.HasIndex(entry => entry.CreatedAt);
            entity.HasIndex(entry => entry.TargetUserId);
            entity.HasIndex(entry => entry.Action);

            entity.Property(entry => entry.ActorUserId).HasMaxLength(450).IsRequired();
            entity.Property(entry => entry.ActorDisplayName).HasMaxLength(256);
            entity.Property(entry => entry.TargetUserId).HasMaxLength(450);
            entity.Property(entry => entry.TargetDisplayName).HasMaxLength(256);
            entity.Property(entry => entry.Action).HasMaxLength(64).IsRequired();
            entity.Property(entry => entry.Details).HasMaxLength(2048);
            entity.Property(entry => entry.IpAddress).HasMaxLength(64);
        });

        var ticketTypeArrayConverter = CreateTicketTypeArrayConverter();

        builder.Entity<TicketCategory>(entity =>
        {
            entity.HasKey(category => category.Id);
            entity.Property(category => category.Id).HasMaxLength(64).IsRequired();
            entity.Property(category => category.Label).HasMaxLength(128).IsRequired();
            entity.Property(category => category.AppliesTo)
                .HasConversion(ticketTypeArrayConverter)
                .HasMaxLength(256)
                .IsRequired();
            entity.HasIndex(category => category.SortOrder);
        });

        builder.Entity<SupportTicket>(entity =>
        {
            entity.HasKey(ticket => ticket.Id);
            entity.HasIndex(ticket => ticket.SubmittedByUserId);
            entity.HasIndex(ticket => ticket.AssignedToUserId);
            entity.HasIndex(ticket => new { ticket.Status, ticket.Type, ticket.Priority });
            entity.HasIndex(ticket => ticket.CreatedAt);

            entity.Property(ticket => ticket.Subject).HasMaxLength(200).IsRequired();
            entity.Property(ticket => ticket.Description).HasMaxLength(4000).IsRequired();
            entity.Property(ticket => ticket.CategoryId).HasMaxLength(64).IsRequired();
            entity.Property(ticket => ticket.BugSeverity).HasMaxLength(32);
            entity.Property(ticket => ticket.StepsToReproduce).HasMaxLength(4000);
            entity.Property(ticket => ticket.ExpectedBehavior).HasMaxLength(1000);
            entity.Property(ticket => ticket.ActualBehavior).HasMaxLength(1000);
            entity.Property(ticket => ticket.BrowserOrEnvironment).HasMaxLength(512);
            entity.Property(ticket => ticket.SubmittedByUserId).HasMaxLength(450).IsRequired();
            entity.Property(ticket => ticket.AssignedToUserId).HasMaxLength(450);
            entity.Property(ticket => ticket.AdminNotes).HasMaxLength(4000);

            entity.HasOne(ticket => ticket.Category)
                .WithMany(category => category.Tickets)
                .HasForeignKey(ticket => ticket.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(ticket => ticket.SubmittedBy)
                .WithMany()
                .HasForeignKey(ticket => ticket.SubmittedByUserId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(ticket => ticket.AssignedTo)
                .WithMany()
                .HasForeignKey(ticket => ticket.AssignedToUserId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        builder.Entity<HelpArticle>(entity =>
        {
            entity.HasKey(article => article.Id);
            entity.HasIndex(article => article.Slug).IsUnique();
            entity.HasIndex(article => new { article.IsPublished, article.CategoryName });

            entity.Property(article => article.Title).HasMaxLength(200).IsRequired();
            entity.Property(article => article.Slug).HasMaxLength(220).IsRequired();
            entity.Property(article => article.Body).HasMaxLength(50000).IsRequired();
            entity.Property(article => article.CategoryName).HasMaxLength(128);
            entity.Property(article => article.CreatedByUserId).HasMaxLength(450).IsRequired();

            entity.HasOne(article => article.CreatedBy)
                .WithMany()
                .HasForeignKey(article => article.CreatedByUserId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }

    private static void ConfigureAuditable<TEntity>(EntityTypeBuilder<TEntity> entity)
        where TEntity : class, IAuditable
    {
        entity.Property(item => item.CreatedByUserId).HasMaxLength(450);
        entity.Property(item => item.UpdatedByUserId).HasMaxLength(450);

        ((EntityTypeBuilder)entity)
            .HasOne(typeof(ApplicationUser), "CreatedByUser")
            .WithMany()
            .HasForeignKey("CreatedByUserId")
            .OnDelete(DeleteBehavior.SetNull);

        ((EntityTypeBuilder)entity)
            .HasOne(typeof(ApplicationUser), "UpdatedByUser")
            .WithMany()
            .HasForeignKey("UpdatedByUserId")
            .OnDelete(DeleteBehavior.SetNull);
    }

    private static ValueConverter<TicketType[], string> CreateTicketTypeArrayConverter()
    {
        var jsonOptions = new JsonSerializerOptions
        {
            Converters = { new JsonStringEnumConverter() },
        };

        return new ValueConverter<TicketType[], string>(
            value => JsonSerializer.Serialize(value, jsonOptions),
            value => JsonSerializer.Deserialize<TicketType[]>(value, jsonOptions) ?? Array.Empty<TicketType>());
    }
}
