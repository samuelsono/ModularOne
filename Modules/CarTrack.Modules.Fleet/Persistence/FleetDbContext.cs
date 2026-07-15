using CarTrack.Core;
using CarTrack.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CarTrack.Modules.Fleet;

public sealed class FleetDbContext(DbContextOptions<FleetDbContext> options) : ModuleDbContext(options)
{
    public DbSet<Vehicle> Vehicles => Set<Vehicle>();

    public DbSet<CarTrackPageCache> CarTrackPageCache => Set<CarTrackPageCache>();

    public DbSet<Driver> Drivers => Set<Driver>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<Vehicle>(entity =>
        {
            entity.ToTable("Vehicles");
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

            // CreatedByUserId / UpdatedByUserId are opaque; no cross-module FK to AspNetUsers.
            ConfigureAuditable(entity);
        });

        builder.Entity<CarTrackPageCache>(entity =>
        {
            entity.ToTable("CarTrackPageCache");
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
            entity.ToTable("Drivers");
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
    }

    private static void ConfigureAuditable<TEntity>(EntityTypeBuilder<TEntity> entity)
        where TEntity : class, IAuditable
    {
        entity.Property(item => item.CreatedByUserId).HasMaxLength(450);
        entity.Property(item => item.UpdatedByUserId).HasMaxLength(450);
    }
}
