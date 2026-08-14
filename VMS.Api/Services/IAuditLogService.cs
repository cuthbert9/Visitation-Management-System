using VisitorManagementSystem.Domain.Enums;

namespace VisitorManagementSystem.Api.Services;

public interface IAuditLogService
{
    Task LogAsync(
        AuditAction action,
        string entityType,
        int? entityId,
        int? userId,
        string? description = null,
        string? oldValue = null,
        string? newValue = null,
        ActorType actorType = ActorType.User,
        CancellationToken cancellationToken = default);
}
