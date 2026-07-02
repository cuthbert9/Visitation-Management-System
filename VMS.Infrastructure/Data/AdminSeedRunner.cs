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

        var normalizedEmail = email.Trim().ToLowerInvariant();
        var now = DateTime.UtcNow;

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

        var existingUser = await context.Users
            .FirstOrDefaultAsync(user => user.Email.ToLower() == normalizedEmail, cancellationToken);

        if (existingUser is not null)
        {
            if (existingUser.RoleId != adminRole.Id)
            {
                existingUser.RoleId = adminRole.Id;
                existingUser.UpdatedAt = now;
                await context.SaveChangesAsync(cancellationToken);
            }

            return existingUser.Id;
        }

        var adminUser = new User
        {
            FullName = fullName.Trim(),
            Email = normalizedEmail,
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
