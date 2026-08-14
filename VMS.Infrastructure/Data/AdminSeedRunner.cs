using Microsoft.EntityFrameworkCore;
using VisitorManagementSystem.Domain.Entities;


namespace VisitorManagementSystem.Infrastructure.Data;

public static class AdminSeedRunner
{
    public static async Task<int> SeedSingleAdminAsync(
        AppDbContext context,
        string fullName,
        string email,
        string password,
        CancellationToken cancellationToken = default)
    {
        await context.Database.MigrateAsync(cancellationToken);

        var existingUser = await context.Users.FirstOrDefaultAsync(cancellationToken);
        if (existingUser is not null)
        {
            return existingUser.Id;
        }

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

        var now = DateTime.UtcNow;
        var adminUser = new User
        {
            FullName = fullName.Trim(),
            Email = email.Trim().ToLowerInvariant(),
            PasswordHash = password,
            RoleId = adminRole.Id,
            CreatedAt = now,
            UpdatedAt = now
        };

        context.Users.Add(adminUser);
        await context.SaveChangesAsync(cancellationToken);

        return adminUser.Id;
    }
}
