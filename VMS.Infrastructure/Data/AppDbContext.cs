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
    public DbSet<Department> Departments => Set<Department>();
    public DbSet<Employee> Employees => Set<Employee>();
    public DbSet<Visit> Visits => Set<Visit>();
    public DbSet<VisitItem> VisitItems => Set<VisitItem>();
    public DbSet<VisitStatusHistory> VisitStatusHistories => Set<VisitStatusHistory>();
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
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
            entity.Property(visitor => visitor.FullName).IsRequired().HasMaxLength(150);
            entity.Property(visitor => visitor.PhoneNumber).IsRequired().HasMaxLength(20);
            entity.Property(visitor => visitor.Email).HasMaxLength(150);
            entity.Property(visitor => visitor.IdentificationType)
                .HasConversion<string>()
                .HasMaxLength(30)
                .IsRequired();
            entity.Property(visitor => visitor.IdentificationNumber).IsRequired().HasMaxLength(50);
            entity.Property(visitor => visitor.Organization).HasMaxLength(150);
            entity.Property(visitor => visitor.PhotoUrl).HasMaxLength(500);
            entity.Property(visitor => visitor.CreatedAt).IsRequired();
            entity.Property(visitor => visitor.UpdatedAt).IsRequired();

            entity.HasIndex(visitor => visitor.IdentificationNumber);
            entity.HasIndex(visitor => visitor.PhoneNumber);
        });

        modelBuilder.Entity<Department>(entity =>
        {
            entity.HasKey(department => department.Id);
            entity.Property(department => department.Code).IsRequired().HasMaxLength(30);
            entity.Property(department => department.Name).IsRequired().HasMaxLength(100);
            entity.Property(department => department.Description).HasMaxLength(500);
            entity.Property(department => department.CreatedAt).IsRequired();
            entity.Property(department => department.UpdatedAt).IsRequired();

            entity.HasIndex(department => department.Code).IsUnique();
        });

        modelBuilder.Entity<Employee>(entity =>
        {
            entity.HasKey(employee => employee.Id);
            entity.Property(employee => employee.EmployeeNumber).IsRequired().HasMaxLength(50);
            entity.Property(employee => employee.FullName).IsRequired().HasMaxLength(150);
            entity.Property(employee => employee.Email).HasMaxLength(150);
            entity.Property(employee => employee.PhoneNumber).HasMaxLength(20);
            entity.Property(employee => employee.Position).HasMaxLength(100);
            entity.Property(employee => employee.CreatedAt).IsRequired();
            entity.Property(employee => employee.UpdatedAt).IsRequired();

            entity.HasIndex(employee => employee.EmployeeNumber).IsUnique();
            entity.HasIndex(employee => employee.DepartmentId);
            entity.HasIndex(employee => employee.FullName);

            entity.HasOne(employee => employee.Department)
                .WithMany(department => department.Employees)
                .HasForeignKey(employee => employee.DepartmentId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Visit>(entity =>
        {
            entity.HasKey(visit => visit.Id);
            entity.Property(visit => visit.VisitNumber).IsRequired().HasMaxLength(30);
            entity.Property(visit => visit.PurposeDescription).HasMaxLength(500);
            entity.Property(visit => visit.VehicleModel).HasMaxLength(100);
            entity.Property(visit => visit.VehiclePlateNumber).HasMaxLength(30);
            entity.Property(visit => visit.BadgeNumber).HasMaxLength(30);
            entity.Property(visit => visit.ExitSignatureReference).HasMaxLength(500);
            entity.Property(visit => visit.Remarks).HasMaxLength(1000);
            entity.Property(visit => visit.AttachmentUrl).HasMaxLength(500);
            entity.Property(visit => visit.ArrivalTime).IsRequired();
            entity.Property(visit => visit.CreatedAt).IsRequired();
            entity.Property(visit => visit.UpdatedAt).IsRequired();

            entity.Property(visit => visit.Purpose)
                .HasConversion<string>()
                .HasMaxLength(30)
                .IsRequired();

            entity.Property(visit => visit.Status)
                .HasConversion<string>()
                .HasMaxLength(30)
                .IsRequired();

            entity.Property(visit => visit.StaffAvailabilityStatus)
                .HasConversion<string>()
                .HasMaxLength(30);

            entity.Property(visit => visit.BadgeStatus)
                .HasConversion<string>()
                .HasMaxLength(30);

            entity.HasIndex(visit => visit.VisitNumber).IsUnique();
            entity.HasIndex(visit => visit.VisitorId);
            entity.HasIndex(visit => visit.HostEmployeeId);
            entity.HasIndex(visit => visit.DepartmentId);
            entity.HasIndex(visit => visit.Status);
            entity.HasIndex(visit => visit.ArrivalTime);
            entity.HasIndex(visit => visit.ExpectedDepartureTime);

            entity.HasOne(visit => visit.Visitor)
                .WithMany(visitor => visitor.Visits)
                .HasForeignKey(visit => visit.VisitorId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(visit => visit.HostEmployee)
                .WithMany(employee => employee.HostedVisits)
                .HasForeignKey(visit => visit.HostEmployeeId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(visit => visit.Department)
                .WithMany(department => department.Visits)
                .HasForeignKey(visit => visit.DepartmentId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(visit => visit.CheckedInBy)
                .WithMany(user => user.CheckedInVisits)
                .HasForeignKey(visit => visit.CheckedInById)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(visit => visit.CheckedOutBy)
                .WithMany(user => user.CheckedOutVisits)
                .HasForeignKey(visit => visit.CheckedOutById)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<VisitItem>(entity =>
        {
            entity.HasKey(item => item.Id);
            entity.Property(item => item.ItemName).IsRequired().HasMaxLength(100);
            entity.Property(item => item.Description).HasMaxLength(500);
            entity.Property(item => item.SerialNumber).HasMaxLength(100);
            entity.Property(item => item.Remarks).HasMaxLength(500);
            entity.Property(item => item.Quantity).IsRequired();
            entity.Property(item => item.CreatedAt).IsRequired();

            entity.Property(item => item.ItemType)
                .HasConversion<string>()
                .HasMaxLength(30);

            entity.Property(item => item.MovementType)
                .HasConversion<string>()
                .HasMaxLength(30);

            entity.HasIndex(item => item.VisitId);

            entity.HasOne(item => item.Visit)
                .WithMany(visit => visit.Items)
                .HasForeignKey(item => item.VisitId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<VisitStatusHistory>(entity =>
        {
            entity.HasKey(history => history.Id);
            entity.Property(history => history.Remarks).HasMaxLength(500);
            entity.Property(history => history.ChangedAt).IsRequired();

            entity.Property(history => history.FromStatus)
                .HasConversion<string>()
                .HasMaxLength(30);

            entity.Property(history => history.ToStatus)
                .HasConversion<string>()
                .HasMaxLength(30)
                .IsRequired();

            entity.HasIndex(history => history.VisitId);
            entity.HasIndex(history => history.ToStatus);
            entity.HasIndex(history => history.ChangedAt);

            entity.HasOne(history => history.Visit)
                .WithMany(visit => visit.StatusHistory)
                .HasForeignKey(history => history.VisitId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(history => history.ChangedByUser)
                .WithMany(user => user.VisitStatusChanges)
                .HasForeignKey(history => history.ChangedByUserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Notification>(entity =>
        {
            entity.HasKey(notification => notification.Id);
            entity.Property(notification => notification.Title).IsRequired().HasMaxLength(150);
            entity.Property(notification => notification.Message).IsRequired().HasMaxLength(1000);
            entity.Property(notification => notification.ErrorMessage).HasMaxLength(1000);
            entity.Property(notification => notification.RetryCount).IsRequired();
            entity.Property(notification => notification.CreatedAt).IsRequired();

            entity.Property(notification => notification.Type)
                .HasConversion<string>()
                .HasMaxLength(30)
                .IsRequired();

            entity.Property(notification => notification.Channel)
                .HasConversion<string>()
                .HasMaxLength(30)
                .IsRequired();

            entity.Property(notification => notification.Status)
                .HasConversion<string>()
                .HasMaxLength(30)
                .IsRequired();

            entity.HasIndex(notification => notification.VisitId);
            entity.HasIndex(notification => notification.RecipientEmployeeId);
            entity.HasIndex(notification => notification.Status);
            entity.HasIndex(notification => notification.CreatedAt);

            entity.HasOne(notification => notification.Visit)
                .WithMany(visit => visit.Notifications)
                .HasForeignKey(notification => notification.VisitId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(notification => notification.RecipientEmployee)
                .WithMany(employee => employee.Notifications)
                .HasForeignKey(notification => notification.RecipientEmployeeId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(notification => notification.RecipientUser)
                .WithMany(user => user.ReceivedNotifications)
                .HasForeignKey(notification => notification.RecipientUserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<AuditLog>(entity =>
        {
            entity.HasKey(auditLog => auditLog.Id);
            entity.Property(auditLog => auditLog.EntityType).HasMaxLength(100);
            entity.Property(auditLog => auditLog.Description).HasMaxLength(1000);
            entity.Property(auditLog => auditLog.IpAddress).HasMaxLength(45);
            entity.Property(auditLog => auditLog.UserAgent).HasMaxLength(500);
            entity.Property(auditLog => auditLog.CorrelationId).HasMaxLength(100);
            entity.Property(auditLog => auditLog.CreatedAt).IsRequired();

            entity.Property(auditLog => auditLog.Action)
                .HasConversion<string>()
                .HasMaxLength(30)
                .IsRequired();

            entity.Property(auditLog => auditLog.ActorType)
                .HasConversion<string>()
                .HasMaxLength(30)
                .IsRequired();

            entity.HasIndex(auditLog => auditLog.UserId);
            entity.HasIndex(auditLog => auditLog.EntityType);
            entity.HasIndex(auditLog => auditLog.EntityId);
            entity.HasIndex(auditLog => auditLog.Action);
            entity.HasIndex(auditLog => auditLog.CreatedAt);

            entity.HasOne(auditLog => auditLog.User)
                .WithMany(user => user.AuditLogs)
                .HasForeignKey(auditLog => auditLog.UserId)
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
