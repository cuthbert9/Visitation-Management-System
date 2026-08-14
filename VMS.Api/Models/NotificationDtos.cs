using System.ComponentModel.DataAnnotations;
using VisitorManagementSystem.Domain.Enums;

namespace VisitorManagementSystem.Api.Models;

public class NotificationDto
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
}

public class CreateNotificationDto
{
    public int? VisitId { get; set; }
    public int? RecipientEmployeeId { get; set; }
    public int? RecipientUserId { get; set; }

    [Required]
    public NotificationType Type { get; set; }

    [Required]
    public NotificationChannel Channel { get; set; }

    [Required]
    public string Title { get; set; } = string.Empty;

    [Required]
    public string Message { get; set; } = string.Empty;
}
