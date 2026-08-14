namespace VisitorManagementSystem.Domain.Enums;

public enum VisitStatus
{
    Registered = 1,
    WaitingForHost = 2,
    HostAcknowledged = 3,
    Attended = 4,
    AwaitingExit = 5,
    Completed = 6,
    Closed = 7,
    Cancelled = 8,
    Denied = 9
}
