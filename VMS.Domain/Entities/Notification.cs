using VisitorManagementSystem.Domain.Enums;

namespace VisitorManagementSystem.Domain.Entities;

public class Notification
{
    public int Id { get; set; }

    public int? VisitId { get; set; }
    public int? RecipientEmployeeId { get; set; }
    public int? RecipientUserId { get; set; }

    public NotificationType Type { get; set; }
    public NotificationChannel Channel { get; set; }

    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;

    public NotificationStatus Status { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime? SentAt { get; set; }
    public DateTime? ReadAt { get; set; }
    public DateTime? FailedAt { get; set; }

    public int RetryCount { get; set; }
    public string? ErrorMessage { get; set; }

    public Visit? Visit { get; set; }
    public Employee? RecipientEmployee { get; set; }
    public User? RecipientUser { get; set; }
}
