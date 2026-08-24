using Microsoft.EntityFrameworkCore;
using VisitorManagementSystem.Domain.Entities;

namespace VisitorManagementSystem.Infrastructure.Data;

public static class AdminSeedRunner
{
    private static readonly (string Number, string FullName, string Position)[] SeedEmployees =
    [
        ("EMP-DG-01", "Grace Mwangi", "DirectorGeneral"),
        ("EMP-DIR-01", "Daniel Okafor", "Director"),
        ("EMP-MGR-01", "Fatima Ali", "Manager"),
        ("EMP-OFF-01", "Peter Kimani", "Officer"),
    ];

    // Order matters: Users.Id auto-increments from the one existing seed row (Id=1,
    // matching Receptionist's MockRoleCatalog ActorId). These three land on 2/3/4 in
    // insertion order, matching Security/Admin(Host)/SalesPersonnel's ActorId exactly.
    private static readonly (string Email, string FullName)[] SeedUsers =
    [
        ("security.demo@vms.local", "Security Demo User"),
        ("host.demo@vms.local", "Host Demo User"),
        ("sales.demo@vms.local", "Sales Personnel Demo User"),
    ];

    public static async Task SeedEmployeePositionsAsync(
        AppDbContext context,
        CancellationToken cancellationToken = default)
    {
        await context.Database.MigrateAsync(cancellationToken);

        var department = await context.Departments
            .FirstOrDefaultAsync(department => department.Code == "EXEC", cancellationToken);

        if (department is null)
        {
            var seedNow = DateTime.UtcNow;
            department = new Department
            {
                Code = "EXEC",
                Name = "Executive Office",
                CreatedAt = seedNow,
                UpdatedAt = seedNow
            };

            context.Departments.Add(department);
            await context.SaveChangesAsync(cancellationToken);
        }

        foreach (var seed in SeedEmployees)
        {
            var existing = await context.Employees
                .FirstOrDefaultAsync(employee => employee.EmployeeNumber == seed.Number, cancellationToken);

            if (existing is null)
            {
                var now = DateTime.UtcNow;
                context.Employees.Add(new Employee
                {
                    EmployeeNumber = seed.Number,
                    FullName = seed.FullName,
                    Position = seed.Position,
                    DepartmentId = department.Id,
                    IsActive = true,
                    CreatedAt = now,
                    UpdatedAt = now
                });
            }
            else if (existing.DepartmentId != department.Id)
            {
                existing.DepartmentId = department.Id;
                existing.UpdatedAt = DateTime.UtcNow;
            }
        }

        await context.SaveChangesAsync(cancellationToken);
    }

    public static async Task SeedDemoUsersAsync(
        AppDbContext context,
        CancellationToken cancellationToken = default)
    {
        await context.Database.MigrateAsync(cancellationToken);

        var adminRole = await context.Roles
            .FirstOrDefaultAsync(role => role.Name == "Admin", cancellationToken);

        if (adminRole is null)
        {
            adminRole = new Role
            {
                Name = "Admin",
                Description = "System administrator"
            };

            context.Roles.Add(adminRole);
            await context.SaveChangesAsync(cancellationToken);
        }

        foreach (var seed in SeedUsers)
        {
            var exists = await context.Users
                .AnyAsync(user => user.Email == seed.Email, cancellationToken);

            if (exists)
            {
                continue;
            }

            var now = DateTime.UtcNow;
            context.Users.Add(new User
            {
                FullName = seed.FullName,
                Email = seed.Email,
                PasswordHash = "demo-not-a-real-login",
                RoleId = adminRole.Id,
                CreatedAt = now,
                UpdatedAt = now
            });
        }

        await context.SaveChangesAsync(cancellationToken);
    }
}
