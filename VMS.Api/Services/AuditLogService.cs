using VisitorManagementSystem.Domain.Entities;
using VisitorManagementSystem.Domain.Enums;
using VisitorManagementSystem.Infrastructure.Data;

namespace VisitorManagementSystem.Api.Services;

public class AuditLogService : IAuditLogService
{
    private readonly AppDbContext _context;

    public AuditLogService(AppDbContext context)
    {
        _context = context;
    }

    public async Task LogAsync(
        AuditAction action,
        string entityType,
        int? entityId,
        int? userId,
        string? description = null,
        string? oldValue = null,
        string? newValue = null,
        ActorType actorType = ActorType.User,
        CancellationToken cancellationToken = default)
    {
        _context.AuditLogs.Add(new AuditLog
        {
            UserId = userId,
            ActorType = actorType,
            Action = action,
            EntityType = entityType,
            EntityId = entityId,
            OldValue = oldValue,
            NewValue = newValue,
            Description = description,
            CreatedAt = DateTime.UtcNow
        });

        await _context.SaveChangesAsync(cancellationToken);
    }
}
