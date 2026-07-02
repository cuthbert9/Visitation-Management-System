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
    public ICollection<Visit> CreatedVisits { get; set; } = new List<Visit>();
    public ICollection<Visit> ApprovedVisits { get; set; } = new List<Visit>();
    public ICollection<VisitCheckIn> VisitCheckIns { get; set; } = new List<VisitCheckIn>();
    public ICollection<VisitCheckOut> VisitCheckOuts { get; set; } = new List<VisitCheckOut>();
}
