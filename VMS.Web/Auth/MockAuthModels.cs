namespace VMS.Web.Auth;

public enum DemoRole
{
    Security,
    Receptionist,
    Admin,
    SalesPersonnel
}

public static class DemoPermissions
{
    public const string VisitorsRead = "visitors.read";
    public const string VisitorsCreate = "visitors.create";
    public const string VisitorsEdit = "visitors.edit";
    public const string EmployeesRead = "employees.read";
    public const string EmployeesManage = "employees.manage";
    public const string VisitsRead = "visits.read";
    public const string VisitsCreate = "visits.create";
    public const string VisitsNotifyHost = "visits.notify_host";
    public const string VisitsHostAcknowledge = "visits.host_acknowledge";
    public const string VisitsMarkAttended = "visits.mark_attended";
    public const string VisitsHostComplete = "visits.host_complete";
    public const string VisitsCheckOut = "visits.checkout";
    public const string VisitsClose = "visits.close";
    public const string VisitsCancel = "visits.cancel";
    public const string DepartmentsRead = "departments.read";
    public const string DepartmentsManage = "departments.manage";
    public const string ParkingRead = "parking.read";
    public const string ParkingManageSlots = "parking.manage_slots";
    public const string ParkingReserve = "parking.reserve";
    public const string ParkingRelease = "parking.release";
    public const string OutboundVisitsRead = "outbound_visits.read";
    public const string OutboundVisitsCreate = "outbound_visits.create";
    public const string OutboundVisitsApprove = "outbound_visits.approve";
    public const string SecurityDashboardRead = "security.dashboard.read";
    public const string GateVisitorsRead = "security.gate_visitors.read";
    public const string GateVisitorsCreate = "security.gate_visitors.create";
    public const string SecurityReportsRead = "security.reports.read";
}

public sealed class MockSession
{
    public DemoRole Role { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public int ActorId { get; set; }
    public HashSet<string> Permissions { get; set; } = [];
}

public static class MockRoleCatalog
{
    public static MockSession CreateSession(DemoRole role)
    {
        return role switch
        {
            DemoRole.Security => new MockSession
            {
                Role = role,
                DisplayName = " Security",
                ActorId = 2,
                Permissions =
                [
                    DemoPermissions.SecurityDashboardRead,
                    DemoPermissions.GateVisitorsRead,
                    DemoPermissions.GateVisitorsCreate,
                    DemoPermissions.SecurityReportsRead,
                    DemoPermissions.ParkingRead,
                    DemoPermissions.ParkingReserve,
                    DemoPermissions.ParkingRelease,
                    DemoPermissions.VisitsCheckOut
                ]
            },
            DemoRole.Receptionist => new MockSession
            {
                Role = role,
                DisplayName = " Receptionist",
                ActorId = 1,
                Permissions =
                [
                    DemoPermissions.VisitorsRead,
                    DemoPermissions.VisitorsCreate,
                    DemoPermissions.VisitorsEdit,
                    DemoPermissions.EmployeesRead,
                    DemoPermissions.VisitsRead,
                    DemoPermissions.VisitsCreate,
                    DemoPermissions.VisitsNotifyHost,
                    DemoPermissions.VisitsMarkAttended,
                    DemoPermissions.VisitsCheckOut,
                    DemoPermissions.VisitsClose,
                    DemoPermissions.VisitsCancel,
                    DemoPermissions.DepartmentsRead,
                    DemoPermissions.ParkingRead,
                    DemoPermissions.ParkingReserve,
                    DemoPermissions.ParkingRelease,
                   
                ]
            },
            DemoRole.SalesPersonnel => new MockSession
            {
                Role = role,
                DisplayName = " Sales Personnel",
                ActorId = 4,
                Permissions =
                [
                    DemoPermissions.OutboundVisitsRead,
                    DemoPermissions.OutboundVisitsCreate,
                    DemoPermissions.OutboundVisitsApprove
                ]
            },
            _ => new MockSession
            {
                Role = DemoRole.Admin,
                DisplayName = " Admin",
                ActorId = 3,
                Permissions =
                [
                    
                    DemoPermissions.VisitorsCreate,
                    DemoPermissions.VisitorsEdit,                    
                    DemoPermissions.EmployeesManage,
                    DemoPermissions.VisitsRead,
                    DemoPermissions.VisitsHostAcknowledge,
                    DemoPermissions.VisitsHostComplete,
                    DemoPermissions.VisitsCancel,
                    DemoPermissions.DepartmentsRead,
                    DemoPermissions.DepartmentsManage,
                    DemoPermissions.ParkingRead,
                    DemoPermissions.ParkingManageSlots,
                    DemoPermissions.ParkingReserve,
                    DemoPermissions.ParkingRelease,
                   
                ]
            }
        };
    }
}
