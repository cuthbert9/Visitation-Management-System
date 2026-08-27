using VisitorManagementSystem.Domain.Enums;

namespace VisitorManagementSystem.Domain.Entities;

public class AuditLog
{
    public int Id { get; set; }

    public int? UserId { get; set; }
    public ActorType ActorType { get; set; } = ActorType.User;

    public AuditAction Action { get; set; }
    public string? EntityType { get; set; }
    public int? EntityId { get; set; }

    public string? OldValue { get; set; }
    public string? NewValue { get; set; }
    public string? Description { get; set; }

    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    public string? CorrelationId { get; set; }

    public DateTime CreatedAt { get; set; }

    public User? User { get; set; }
}
