using Microsoft.EntityFrameworkCore;
using VisitorManagementSystem.Domain.Entities;

namespace VisitorManagementSystem.Infrastructure.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public DbSet<Visitor> Visitors => Set<Visitor>();
    public DbSet<Office> Offices => Set<Office>();
    public DbSet<Visit> Visits => Set<Visit>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Visitor>(entity =>
        {
            entity.HasKey(v => v.Id);
            entity.Property(v => v.FullName).IsRequired().HasMaxLength(200);
            entity.Property(v => v.PhoneNumber).HasMaxLength(50);
            entity.Property(v => v.NationalId).HasMaxLength(100);
            entity.Property(v => v.CreatedAt).IsRequired();
        });

        modelBuilder.Entity<Office>(entity =>
        {
            entity.HasKey(o => o.Id);
            entity.Property(o => o.Name).IsRequired().HasMaxLength(200);
            entity.Property(o => o.Location).HasMaxLength(200);
            entity.Property(o => o.Status)
                .HasConversion<string>()
                .HasMaxLength(50)
                .IsRequired();
        });

        modelBuilder.Entity<Visit>(entity =>
        {
            entity.HasKey(v => v.Id);
            entity.Property(v => v.VisitDate).IsRequired();
            entity.Property(v => v.Status)
                .HasConversion<string>()
                .HasMaxLength(50)
                .IsRequired();
            entity.Property(v => v.Purpose).HasMaxLength(500);
            entity.Property(v => v.CreatedAt).IsRequired();

            entity.HasOne(v => v.Visitor)
                .WithMany(v => v.Visits)
                .HasForeignKey(v => v.VisitorId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(v => v.Office)
                .WithMany(o => o.Visits)
                .HasForeignKey(v => v.OfficeId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }
}
