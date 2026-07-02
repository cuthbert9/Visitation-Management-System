namespace VMS.Web.Auth;

public enum DemoRole
{
    Security,
    Receptionist,
    Admin
}

public static class DemoPermissions
{
    public const string VisitorsRead = "visitors.read";
    public const string VisitorsCreate = "visitors.create";
    public const string VisitorsEdit = "visitors.edit";
    public const string OfficesRead = "offices.read";
    public const string OfficesManage = "offices.manage";
    public const string VisitsRead = "visits.read";
    public const string VisitsCreate = "visits.create";
    public const string VisitsApprove = "visits.approve";
    public const string VisitsReject = "visits.reject";
    public const string VisitsCheckIn = "visits.checkin";
    public const string VisitsCheckOut = "visits.checkout";
    public const string DepartmentsRead = "departments.read";
    public const string DepartmentsManage = "departments.manage";
    public const string ParkingRead = "parking.read";
    public const string ParkingManageSlots = "parking.manage_slots";
    public const string ParkingReserve = "parking.reserve";
    public const string ParkingRelease = "parking.release";
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
                DisplayName = "Demo Security",
                ActorId = 2,
                Permissions =
                [
                    DemoPermissions.VisitsRead,
                    DemoPermissions.VisitsCheckIn,
                    DemoPermissions.VisitsCheckOut,
                    DemoPermissions.ParkingRead,
                    DemoPermissions.ParkingRelease
                ]
            },
            DemoRole.Receptionist => new MockSession
            {
                Role = role,
                DisplayName = "Demo Receptionist",
                ActorId = 1,
                Permissions =
                [
                    DemoPermissions.VisitorsRead,
                    DemoPermissions.VisitorsCreate,
                    DemoPermissions.VisitorsEdit,
                    DemoPermissions.OfficesRead,
                    DemoPermissions.VisitsRead,
                    DemoPermissions.VisitsCreate,
                    DemoPermissions.VisitsApprove,
                    DemoPermissions.VisitsReject,
                    DemoPermissions.VisitsCheckIn,
                    DemoPermissions.VisitsCheckOut,
                    DemoPermissions.DepartmentsRead,
                    DemoPermissions.ParkingRead,
                    DemoPermissions.ParkingReserve,
                    DemoPermissions.ParkingRelease
                ]
            },
            _ => new MockSession
            {
                Role = DemoRole.Admin,
                DisplayName = "Demo Admin",
                ActorId = 3,
                Permissions =
                [
                    DemoPermissions.VisitorsRead,
                    DemoPermissions.VisitorsCreate,
                    DemoPermissions.VisitorsEdit,
                    DemoPermissions.OfficesRead,
                    DemoPermissions.OfficesManage,
                    DemoPermissions.VisitsRead,
                    DemoPermissions.VisitsCreate,
                    DemoPermissions.VisitsApprove,
                    DemoPermissions.VisitsReject,
                    DemoPermissions.VisitsCheckIn,
                    DemoPermissions.VisitsCheckOut,
                    DemoPermissions.DepartmentsRead,
                    DemoPermissions.DepartmentsManage,
                    DemoPermissions.ParkingRead,
                    DemoPermissions.ParkingManageSlots,
                    DemoPermissions.ParkingReserve,
                    DemoPermissions.ParkingRelease
                ]
            }
        };
    }
}
