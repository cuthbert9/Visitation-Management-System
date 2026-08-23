using VisitorManagementSystem.Shared.Models;

namespace VMS.Web.Shared;

public static class EnumLabels
{
    // VisitPurposeType has legacy aliases sharing values with their renamed counterparts
    // (e.g. Meeting == OfficialMeeting) so old rows still deserialize — Enum.GetValues would
    // list both and render duplicate-looking options, so dropdowns should enumerate this
    // canonical list instead.
    public static readonly VisitPurposeType[] SelectableVisitPurposes =
    [
        VisitPurposeType.OfficialMeeting,
        VisitPurposeType.Interview,
        VisitPurposeType.Delivery,
        VisitPurposeType.Maintenance,
        VisitPurposeType.Official,
        VisitPurposeType.Personal,
        VisitPurposeType.Facilitator,
        VisitPurposeType.Technician,
        VisitPurposeType.Other,
        VisitPurposeType.ExternalAuditor
    ];

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
        VisitPurposeType.OfficialMeeting => "Official Meeting",
        VisitPurposeType.Interview => "Interview",
        VisitPurposeType.Delivery => "Delivery",
        VisitPurposeType.Maintenance => "Maintenance",
        VisitPurposeType.Official => "Official",
        VisitPurposeType.Personal => "Personal",
        VisitPurposeType.Facilitator => "Facilitator",
        VisitPurposeType.Technician => "Technician",
        VisitPurposeType.Other => "Other",
        VisitPurposeType.ExternalAuditor => "External Auditor",
        _ => purpose.ToString()
    };

    public static string Format(VisitItemType itemType) => itemType switch
    {
        VisitItemType.Personal => "Bag",
        VisitItemType.Document => "Folder / Documents",
        VisitItemType.Delivery => "Parcel",
        VisitItemType.Tool => "Tools",
        VisitItemType.Equipment => "Equipment",
        VisitItemType.ElectronicDevice => "Electronic Device",
        VisitItemType.Other => "Other",
        _ => itemType.ToString()
    };
}
