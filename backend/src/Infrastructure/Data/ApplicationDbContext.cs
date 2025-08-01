using Core.Entities;
using Infrastructure.Data.Interceptors;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Data;

public class ApplicationDbContext : DbContext
{
    private readonly AuditInterceptor _auditInterceptor;

    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options, AuditInterceptor auditInterceptor)
        : base(options)
    {
        _auditInterceptor = auditInterceptor;
    }

    public DbSet<Car> Cars => Set<Car>();
    public DbSet<OwnerCar> OwnerCars => Set<OwnerCar>();

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.AddInterceptors(_auditInterceptor);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure Cars
        modelBuilder.Entity<Car>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Make).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Model).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Vin).IsRequired().HasMaxLength(17);
            entity.HasIndex(e => e.Vin).IsUnique();
            
            // Soft delete filter
            entity.HasQueryFilter(e => !e.IsDeleted);
        });

        // Configure OwnerCars
        modelBuilder.Entity<OwnerCar>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.OwnerId).IsRequired();
            entity.Property(e => e.OwnershipType).HasMaxLength(50);
            
            entity.HasOne(e => e.Car)
                .WithMany(c => c.OwnerCars)
                .HasForeignKey(e => e.CarId)
                .OnDelete(DeleteBehavior.Cascade);
            
            // Soft delete filter
            entity.HasQueryFilter(e => !e.IsDeleted);
        });

        // Configure base entity properties
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (typeof(AuditableEntity).IsAssignableFrom(entityType.ClrType))
            {
                modelBuilder.Entity(entityType.ClrType)
                    .Property<DateTime>("CreatedAt")
                    .HasDefaultValueSql("GETUTCDATE()");
            }
        }
    }
}