using VisitorManagementSystem.Shared.Models;

namespace VMS.Web.Shared;

public static class EnumLabels
{
    public static string Format(IdentificationType type) => type switch
    {
        IdentificationType.NationalId => "National ID",
        IdentificationType.Passport => "Passport",
        IdentificationType.DriverLicense => "Driver's License",
        IdentificationType.WorkId => "Work ID",
        IdentificationType.Other => "Other",
        _ => type.ToString()
    };

    public static string Format(VisitPurposeType purpose) => purpose switch
    {
        VisitPurposeType.Meeting => "Meeting",
        VisitPurposeType.Interview => "Interview",
        VisitPurposeType.Delivery => "Delivery",
        VisitPurposeType.Maintenance => "Maintenance",
        VisitPurposeType.Official => "Official",
        VisitPurposeType.Personal => "Personal",
        VisitPurposeType.Training => "Training",
        VisitPurposeType.ContractorWork => "Contractor Work",
        VisitPurposeType.Other => "Other",
        _ => purpose.ToString()
    };
}
