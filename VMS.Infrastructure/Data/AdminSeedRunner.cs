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
}
