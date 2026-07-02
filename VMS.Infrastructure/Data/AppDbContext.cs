using Microsoft.EntityFrameworkCore;
using VisitorManagementSystem.Domain.Entities;

namespace VisitorManagementSystem.Infrastructure.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<Permission> Permissions => Set<Permission>();
    public DbSet<RolePermission> RolePermissions => Set<RolePermission>();
    public DbSet<Visitor> Visitors => Set<Visitor>();
    public DbSet<Office> Offices => Set<Office>();
    public DbSet<Department> Departments => Set<Department>();
    public DbSet<Visit> Visits => Set<Visit>();
    public DbSet<VisitCheckIn> VisitCheckIns => Set<VisitCheckIn>();
    public DbSet<VisitCheckOut> VisitCheckOuts => Set<VisitCheckOut>();
    public DbSet<ParkingSlot> ParkingSlots => Set<ParkingSlot>();
    public DbSet<ParkingReservation> ParkingReservations => Set<ParkingReservation>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Role>(entity =>
        {
            entity.HasKey(role => role.Id);
            entity.Property(role => role.Name).IsRequired().HasMaxLength(100);
            entity.Property(role => role.Description).HasMaxLength(500);
        });

        modelBuilder.Entity<Permission>(entity =>
        {
            entity.HasKey(permission => permission.Id);
            entity.Property(permission => permission.Code).IsRequired().HasMaxLength(100);
            entity.Property(permission => permission.Description).HasMaxLength(500);
            entity.HasIndex(permission => permission.Code).IsUnique();
        });

        modelBuilder.Entity<RolePermission>(entity =>
        {
            entity.HasKey(rolePermission => new { rolePermission.RoleId, rolePermission.PermissionId });

            entity.HasOne(rolePermission => rolePermission.Role)
                .WithMany(role => role.RolePermissions)
                .HasForeignKey(rolePermission => rolePermission.RoleId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(rolePermission => rolePermission.Permission)
                .WithMany(permission => permission.RolePermissions)
                .HasForeignKey(rolePermission => rolePermission.PermissionId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(user => user.Id);
            entity.Property(user => user.FullName).IsRequired().HasMaxLength(200);
            entity.Property(user => user.Email).IsRequired().HasMaxLength(200);
            entity.Property(user => user.PasswordHash).IsRequired().HasMaxLength(500);
            entity.Property(user => user.CreatedAt).IsRequired();
            entity.Property(user => user.UpdatedAt).IsRequired();
            entity.HasIndex(user => user.Email).IsUnique();

            entity.HasOne(user => user.Role)
                .WithMany(role => role.Users)
                .HasForeignKey(user => user.RoleId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Visitor>(entity =>
        {
            entity.HasKey(visitor => visitor.Id);
            entity.Property(visitor => visitor.FullName).IsRequired().HasMaxLength(200);
            entity.Property(visitor => visitor.Phone).HasMaxLength(50);
            entity.Property(visitor => visitor.NationalId).HasMaxLength(100);
            entity.Property(visitor => visitor.Company).HasMaxLength(200);
            entity.Property(visitor => visitor.VehiclePlate).HasMaxLength(50);
            entity.Property(visitor => visitor.PhotoUrl).HasMaxLength(500);
            entity.Property(visitor => visitor.CreatedAt).IsRequired();
            entity.Property(visitor => visitor.UpdatedAt).IsRequired();
        });

        modelBuilder.Entity<Office>(entity =>
        {
            entity.HasKey(office => office.Id);
            entity.Property(office => office.Name).IsRequired().HasMaxLength(200);
            entity.Property(office => office.Floor).HasMaxLength(50);
            entity.Property(office => office.OfficeCode).HasMaxLength(100);
            entity.Property(office => office.CreatedAt).IsRequired();
            entity.Property(office => office.UpdatedAt).IsRequired();
            entity.HasIndex(office => office.OfficeCode).IsUnique();
        });

        modelBuilder.Entity<Department>(entity =>
        {
            entity.HasKey(department => department.Id);
            entity.Property(department => department.Name).IsRequired().HasMaxLength(200);
            entity.Property(department => department.Description).HasMaxLength(500);
            entity.Property(department => department.CreatedAt).IsRequired();
            entity.Property(department => department.UpdatedAt).IsRequired();

            entity.HasOne(department => department.Office)
                .WithMany(office => office.Departments)
                .HasForeignKey(department => department.OfficeId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Visit>(entity =>
        {
            entity.HasKey(visit => visit.Id);
            entity.Property(visit => visit.Purpose).HasMaxLength(500);
            entity.Property(visit => visit.AttachmentUrl).HasMaxLength(500);
            entity.Property(visit => visit.VisitDate).IsRequired();
            entity.Property(visit => visit.ExpectedArrival);
            entity.Property(visit => visit.Status)
                .HasConversion<string>()
                .HasMaxLength(50)
                .IsRequired();
            entity.Property(visit => visit.CreatedAt).IsRequired();
            entity.Property(visit => visit.UpdatedAt).IsRequired();

            entity.HasOne(visit => visit.Visitor)
                .WithMany(visitor => visitor.Visits)
                .HasForeignKey(visit => visit.VisitorId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(visit => visit.Office)
                .WithMany(office => office.Visits)
                .HasForeignKey(visit => visit.OfficeId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(visit => visit.CreatedBy)
                .WithMany(user => user.CreatedVisits)
                .HasForeignKey(visit => visit.CreatedById)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(visit => visit.ApprovedBy)
                .WithMany(user => user.ApprovedVisits)
                .HasForeignKey(visit => visit.ApprovedById)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<VisitCheckIn>(entity =>
        {
            entity.HasKey(checkIn => checkIn.Id);
            entity.Property(checkIn => checkIn.CheckInTime).IsRequired();
            entity.Property(checkIn => checkIn.Gate).HasMaxLength(100);
            entity.Property(checkIn => checkIn.BadgeNumber).HasMaxLength(100);
            entity.Property(checkIn => checkIn.CreatedAt).IsRequired();

            entity.HasOne(checkIn => checkIn.Visit)
                .WithMany(visit => visit.CheckIns)
                .HasForeignKey(checkIn => checkIn.VisitId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(checkIn => checkIn.CheckedInBy)
                .WithMany(user => user.VisitCheckIns)
                .HasForeignKey(checkIn => checkIn.CheckedInById)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<VisitCheckOut>(entity =>
        {
            entity.HasKey(checkOut => checkOut.Id);
            entity.Property(checkOut => checkOut.CheckOutTime).IsRequired();
            entity.Property(checkOut => checkOut.Remarks).HasMaxLength(1000);
            entity.Property(checkOut => checkOut.CreatedAt).IsRequired();

            entity.HasOne(checkOut => checkOut.Visit)
                .WithMany(visit => visit.CheckOuts)
                .HasForeignKey(checkOut => checkOut.VisitId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(checkOut => checkOut.CheckedOutBy)
                .WithMany(user => user.VisitCheckOuts)
                .HasForeignKey(checkOut => checkOut.CheckedOutById)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<ParkingSlot>(entity =>
        {
            entity.HasKey(slot => slot.Id);
            entity.Property(slot => slot.Code).IsRequired().HasMaxLength(100);
            entity.Property(slot => slot.Zone).HasMaxLength(100);
            entity.Property(slot => slot.Status)
                .HasConversion<string>()
                .HasMaxLength(50)
                .IsRequired();
            entity.Property(slot => slot.CreatedAt).IsRequired();
            entity.HasIndex(slot => slot.Code).IsUnique();
        });

        modelBuilder.Entity<ParkingReservation>(entity =>
        {
            entity.HasKey(reservation => reservation.Id);
            entity.Property(reservation => reservation.VehiclePlate).HasMaxLength(50);
            entity.Property(reservation => reservation.Status)
                .HasConversion<string>()
                .HasMaxLength(50)
                .IsRequired();
            entity.Property(reservation => reservation.CreatedAt).IsRequired();

            entity.HasOne(reservation => reservation.Visit)
                .WithMany(visit => visit.ParkingReservations)
                .HasForeignKey(reservation => reservation.VisitId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(reservation => reservation.Slot)
                .WithMany(slot => slot.Reservations)
                .HasForeignKey(reservation => reservation.SlotId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }
}
