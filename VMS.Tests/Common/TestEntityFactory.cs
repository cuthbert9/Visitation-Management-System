using VisitorManagementSystem.Domain.Entities;
using VisitorManagementSystem.Domain.Enums;
using VisitorManagementSystem.Infrastructure.Data;

namespace VMS.Tests.Common;

/// <summary>
/// Seeds minimal, valid entity graphs into a test <see cref="AppDbContext"/> so controller tests
/// can set up preconditions in one line instead of repeating entity construction everywhere.
/// Each method saves immediately so the returned entity has a real, persisted Id.
/// </summary>
public static class TestEntityFactory
{
    public static Role AddRole(this AppDbContext context, string name = "Admin")
    {
        var role = new Role { Name = name };
        context.Roles.Add(role);
        context.SaveChanges();
        return role;
    }

    public static User AddUser(this AppDbContext context, int? roleId = null, string fullName = "Test User", string? email = null)
    {
        var resolvedRoleId = roleId ?? context.AddRole().Id;
        var now = DateTime.UtcNow;
        var user = new User
        {
            FullName = fullName,
            Email = email ?? $"user-{Guid.NewGuid():N}@example.com",
            PasswordHash = "hashed-password",
            RoleId = resolvedRoleId,
            CreatedAt = now,
            UpdatedAt = now
        };

        context.Users.Add(user);
        context.SaveChanges();
        return user;
    }

    public static Department AddDepartment(this AppDbContext context, string? code = null, string name = "Engineering", bool isActive = true)
    {
        var now = DateTime.UtcNow;
        var department = new Department
        {
            Code = code ?? $"DEPT-{Guid.NewGuid():N}"[..10],
            Name = name,
            IsActive = isActive,
            CreatedAt = now,
            UpdatedAt = now
        };

        context.Departments.Add(department);
        context.SaveChanges();
        return department;
    }

    public static Employee AddEmployee(
        this AppDbContext context,
        int? departmentId = null,
        string? employeeNumber = null,
        string fullName = "Jane Employee",
        string? position = null,
        bool isActive = true)
    {
        var resolvedDepartmentId = departmentId ?? context.AddDepartment().Id;
        var now = DateTime.UtcNow;
        var employee = new Employee
        {
            EmployeeNumber = employeeNumber ?? $"EMP-{Guid.NewGuid():N}"[..10],
            FullName = fullName,
            Position = position,
            DepartmentId = resolvedDepartmentId,
            IsActive = isActive,
            CreatedAt = now,
            UpdatedAt = now
        };

        context.Employees.Add(employee);
        context.SaveChanges();
        return employee;
    }

    public static Visitor AddVisitor(this AppDbContext context, string fullName = "John Visitor", string? email = "visitor@example.com")
    {
        var now = DateTime.UtcNow;
        var visitor = new Visitor
        {
            FullName = fullName,
            PhoneNumber = "0700000000",
            Email = email,
            IdentificationType = IdentificationType.NationalId,
            IdentificationNumber = $"ID-{Guid.NewGuid():N}"[..10],
            IsActive = true,
            CreatedAt = now,
            UpdatedAt = now
        };

        context.Visitors.Add(visitor);
        context.SaveChanges();
        return visitor;
    }

    public static ParkingSlot AddParkingSlot(this AppDbContext context, string? code = null, ParkingSlotStatus status = ParkingSlotStatus.Available)
    {
        var slot = new ParkingSlot
        {
            Code = code ?? $"SLOT-{Guid.NewGuid():N}"[..10],
            Status = status,
            CreatedAt = DateTime.UtcNow
        };

        context.ParkingSlots.Add(slot);
        context.SaveChanges();
        return slot;
    }

    public static ParkingReservation AddParkingReservation(
        this AppDbContext context,
        int? visitId = null,
        int? slotId = null,
        ParkingReservationStatus status = ParkingReservationStatus.Reserved)
    {
        var resolvedVisitId = visitId ?? context.AddVisit().Id;
        var resolvedSlotId = slotId ?? context.AddParkingSlot(status: ParkingSlotStatus.Reserved).Id;
        var now = DateTime.UtcNow;
        var reservation = new ParkingReservation
        {
            VisitId = resolvedVisitId,
            SlotId = resolvedSlotId,
            Status = status,
            ReservedAt = status == ParkingReservationStatus.Reserved ? now : null,
            CreatedAt = now
        };

        context.ParkingReservations.Add(reservation);
        context.SaveChanges();
        return reservation;
    }

    public static Visit AddVisit(
        this AppDbContext context,
        int? visitorId = null,
        int? checkedInById = null,
        VisitStatus status = VisitStatus.GateRegistered,
        int? hostEmployeeId = null,
        int? departmentId = null,
        string? badgeNumber = null)
    {
        var resolvedVisitorId = visitorId ?? context.AddVisitor().Id;
        var resolvedCheckedInById = checkedInById ?? context.AddUser().Id;
        var now = DateTime.UtcNow;
        var visit = new Visit
        {
            VisitNumber = $"V-TEST-{Guid.NewGuid():N}"[..12],
            VisitorId = resolvedVisitorId,
            CheckedInById = resolvedCheckedInById,
            HostEmployeeId = hostEmployeeId,
            DepartmentId = departmentId,
            Status = status,
            ArrivalTime = now,
            BadgeNumber = badgeNumber,
            BadgeStatus = badgeNumber is null ? null : BadgeStatus.Issued,
            CreatedAt = now,
            UpdatedAt = now
        };

        context.Visits.Add(visit);
        context.SaveChanges();
        return visit;
    }
}
