namespace VisitorManagementSystem.Domain.Entities;

public class User
{
    public int Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public int RoleId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public Role Role { get; set; } = null!;
    public ICollection<Visit> CheckedInVisits { get; set; } = new List<Visit>();
    public ICollection<Visit> CheckedOutVisits { get; set; } = new List<Visit>();
    public ICollection<VisitStatusHistory> VisitStatusChanges { get; set; } = new List<VisitStatusHistory>();
    public ICollection<Notification> ReceivedNotifications { get; set; } = new List<Notification>();
    public ICollection<AuditLog> AuditLogs { get; set; } = new List<AuditLog>();
}
