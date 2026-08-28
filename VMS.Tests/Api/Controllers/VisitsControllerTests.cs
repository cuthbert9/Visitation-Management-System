using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using VisitorManagementSystem.Api.Controllers;
using VisitorManagementSystem.Api.Models;
using VisitorManagementSystem.Api.Services;
using VisitorManagementSystem.Domain.Entities;
using VisitorManagementSystem.Domain.Enums;
using VisitorManagementSystem.Infrastructure.Data;
using VMS.Tests.Common;

namespace VMS.Tests.Api.Controllers;

public class VisitsControllerTests : IDisposable
{
    private readonly SqliteInMemoryDbContextFactory _factory = new();
    private readonly AppDbContext _context;
    private readonly Mock<IAuditLogService> _auditLog = new();
    private readonly Mock<IEmailService> _emailService = new();
    private readonly VisitsController _controller;

    public VisitsControllerTests()
    {
        _context = _factory.CreateContext();
        _controller = new VisitsController(_context, _auditLog.Object, _emailService.Object, NullLogger<VisitsController>.Instance);
    }

    public void Dispose()
    {
        _context.Dispose();
        _factory.Dispose();
    }

    private async Task<Visit?> ReloadVisitAsync(int id)
    {
        await using var verifyContext = _factory.CreateContext();
        return await verifyContext.Visits.FindAsync(id);
    }

    private async Task<List<VisitStatusHistory>> ReloadHistoryAsync(int visitId)
    {
        await using var verifyContext = _factory.CreateContext();
        return verifyContext.VisitStatusHistories.Where(h => h.VisitId == visitId).OrderBy(h => h.ChangedAt).ToList();
    }

    private void VerifyAuditLogNeverCalled() => _auditLog.Verify(log => log.LogAsync(
        It.IsAny<AuditAction>(), It.IsAny<string>(), It.IsAny<int?>(), It.IsAny<int?>(),
        It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<ActorType>(), It.IsAny<CancellationToken>()),
        Times.Never);

    private void VerifyAuditLogCalled(AuditAction action, Times times) => _auditLog.Verify(log => log.LogAsync(
        action, nameof(Visit), It.IsAny<int?>(), It.IsAny<int?>(),
        It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<ActorType>(), It.IsAny<CancellationToken>()),
        times);

    private void VerifyEmailNeverSent() => _emailService.Verify(
        email => email.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);

    // ---- Create ----

    [Fact]
    public async Task Create_VisitorMissing_ReturnsBadRequestAndPersistsNothing()
    {
        var checkedInBy = _context.AddUser();

        var result = await _controller.Create(new VisitCreateDto { VisitorId = 999, CheckedInById = checkedInBy.Id });

        Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.Empty(_context.Visits);
        VerifyAuditLogNeverCalled();
        VerifyEmailNeverSent();
    }

    [Fact]
    public async Task Create_HostEmployeeMissing_ReturnsBadRequest()
    {
        var visitor = _context.AddVisitor();
        var checkedInBy = _context.AddUser();

        var result = await _controller.Create(new VisitCreateDto { VisitorId = visitor.Id, CheckedInById = checkedInBy.Id, HostEmployeeId = 999 });

        Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.Empty(_context.Visits);
        VerifyAuditLogNeverCalled();
    }

    [Fact]
    public async Task Create_DepartmentMissing_ReturnsBadRequest()
    {
        var visitor = _context.AddVisitor();
        var checkedInBy = _context.AddUser();

        var result = await _controller.Create(new VisitCreateDto { VisitorId = visitor.Id, CheckedInById = checkedInBy.Id, DepartmentId = 999 });

        Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.Empty(_context.Visits);
        VerifyAuditLogNeverCalled();
    }

    [Fact]
    public async Task Create_CheckedInUserMissing_ReturnsBadRequest()
    {
        var visitor = _context.AddVisitor();

        var result = await _controller.Create(new VisitCreateDto { VisitorId = visitor.Id, CheckedInById = 999 });

        Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.Empty(_context.Visits);
        VerifyAuditLogNeverCalled();
    }

    [Fact]
    public async Task Create_FullyRegistered_SetsStatusRegisteredComputesDurationAndSendsEmail()
    {
        var visitor = _context.AddVisitor(email: "visitor@example.com");
        var department = _context.AddDepartment();
        var host = _context.AddEmployee(departmentId: department.Id, position: "Manager");
        var checkedInBy = _context.AddUser();

        var result = await _controller.Create(new VisitCreateDto
        {
            VisitorId = visitor.Id,
            HostEmployeeId = host.Id,
            DepartmentId = department.Id,
            CheckedInById = checkedInBy.Id,
            Purpose = VisitPurposeType.Official
        });

        var created = Assert.IsType<CreatedAtActionResult>(result.Result);
        var dto = Assert.IsType<VisitDto>(created.Value);
        Assert.Equal(VisitStatus.Registered, dto.Status);
        Assert.NotNull(dto.ExpectedDepartureTime);
        Assert.Equal(dto.ArrivalTime.AddHours(1), dto.ExpectedDepartureTime);

        var history = await ReloadHistoryAsync(dto.Id);
        var entry = Assert.Single(history);
        Assert.Null(entry.FromStatus);
        Assert.Equal(VisitStatus.Registered, entry.ToStatus);
        Assert.Equal(checkedInBy.Id, entry.ChangedByUserId);

        _emailService.Verify(email => email.SendEmailAsync("visitor@example.com", It.IsAny<string>(), It.IsAny<string>()), Times.Once);
        VerifyAuditLogCalled(AuditAction.CheckIn, Times.Once());
    }

    [Fact]
    public async Task Create_PartialInfo_SetsStatusGateRegisteredAndSkipsEmail()
    {
        var visitor = _context.AddVisitor(email: "visitor@example.com");
        var checkedInBy = _context.AddUser();

        var result = await _controller.Create(new VisitCreateDto { VisitorId = visitor.Id, CheckedInById = checkedInBy.Id });

        var created = Assert.IsType<CreatedAtActionResult>(result.Result);
        var dto = Assert.IsType<VisitDto>(created.Value);
        Assert.Equal(VisitStatus.GateRegistered, dto.Status);
        Assert.Null(dto.ExpectedDepartureTime);

        VerifyEmailNeverSent();
        VerifyAuditLogCalled(AuditAction.CheckIn, Times.Once());
    }

    [Fact]
    public async Task Create_WithBadgeNumber_SetsBadgeIssuedAndLogsBadgeAssigned()
    {
        var visitor = _context.AddVisitor();
        var checkedInBy = _context.AddUser();

        var result = await _controller.Create(new VisitCreateDto { VisitorId = visitor.Id, CheckedInById = checkedInBy.Id, BadgeNumber = "B-1" });

        var created = Assert.IsType<CreatedAtActionResult>(result.Result);
        var dto = Assert.IsType<VisitDto>(created.Value);
        Assert.Equal(BadgeStatus.Issued, dto.BadgeStatus);
        Assert.NotNull(dto.BadgeIssuedAt);

        VerifyAuditLogCalled(AuditAction.CheckIn, Times.Once());
        VerifyAuditLogCalled(AuditAction.BadgeAssigned, Times.Once());
    }

    [Fact]
    public async Task Create_WithoutBadgeNumber_DoesNotLogBadgeAssigned()
    {
        var visitor = _context.AddVisitor();
        var checkedInBy = _context.AddUser();

        var result = await _controller.Create(new VisitCreateDto { VisitorId = visitor.Id, CheckedInById = checkedInBy.Id });

        var created = Assert.IsType<CreatedAtActionResult>(result.Result);
        var dto = Assert.IsType<VisitDto>(created.Value);
        Assert.Null(dto.BadgeStatus);

        VerifyAuditLogCalled(AuditAction.BadgeAssigned, Times.Never());
    }

    [Fact]
    public async Task Create_SecondVisitSameDay_IncrementsVisitNumberSuffix()
    {
        var visitor = _context.AddVisitor();
        var checkedInBy = _context.AddUser();

        var first = await _controller.Create(new VisitCreateDto { VisitorId = visitor.Id, CheckedInById = checkedInBy.Id });
        var second = await _controller.Create(new VisitCreateDto { VisitorId = visitor.Id, CheckedInById = checkedInBy.Id });

        var firstDto = Assert.IsType<VisitDto>(((CreatedAtActionResult)first.Result!).Value);
        var secondDto = Assert.IsType<VisitDto>(((CreatedAtActionResult)second.Result!).Value);

        Assert.NotEqual(firstDto.VisitNumber, secondDto.VisitNumber);
        Assert.EndsWith("0001", firstDto.VisitNumber);
        Assert.EndsWith("0002", secondDto.VisitNumber);
        Assert.Equal(2, _context.Visits.Count());
    }

    // ---- CompleteHandover ----

    [Fact]
    public async Task CompleteHandover_NotFound_ReturnsNotFound()
    {
        var result = await _controller.CompleteHandover(999, new CompleteHandoverDto { HostEmployeeId = 1, DepartmentId = 1, Purpose = VisitPurposeType.Meeting });

        Assert.IsType<NotFoundResult>(result.Result);
    }

    [Fact]
    public async Task CompleteHandover_WrongStatus_ReturnsConflictAndLeavesVisitUnchanged()
    {
        var visit = _context.AddVisit(status: VisitStatus.Registered);
        var department = _context.AddDepartment();
        var host = _context.AddEmployee(departmentId: department.Id);

        var result = await _controller.CompleteHandover(visit.Id, new CompleteHandoverDto { HostEmployeeId = host.Id, DepartmentId = department.Id, Purpose = VisitPurposeType.Meeting });

        Assert.IsType<ConflictObjectResult>(result.Result);
        var reloaded = await ReloadVisitAsync(visit.Id);
        Assert.Equal(VisitStatus.Registered, reloaded!.Status);
        Assert.Null(reloaded.HostEmployeeId);
        VerifyAuditLogNeverCalled();
    }

    [Fact]
    public async Task CompleteHandover_HostMissing_ReturnsBadRequestAndLeavesVisitUnchanged()
    {
        var visit = _context.AddVisit(status: VisitStatus.GateRegistered);
        var department = _context.AddDepartment();

        var result = await _controller.CompleteHandover(visit.Id, new CompleteHandoverDto { HostEmployeeId = 999, DepartmentId = department.Id, Purpose = VisitPurposeType.Meeting });

        Assert.IsType<BadRequestObjectResult>(result.Result);
        var reloaded = await ReloadVisitAsync(visit.Id);
        Assert.Equal(VisitStatus.GateRegistered, reloaded!.Status);
        VerifyAuditLogNeverCalled();
    }

    [Fact]
    public async Task CompleteHandover_DepartmentMissing_ReturnsBadRequest()
    {
        var visit = _context.AddVisit(status: VisitStatus.GateRegistered);
        var department = _context.AddDepartment();
        var host = _context.AddEmployee(departmentId: department.Id);

        var result = await _controller.CompleteHandover(visit.Id, new CompleteHandoverDto { HostEmployeeId = host.Id, DepartmentId = 999, Purpose = VisitPurposeType.Meeting });

        Assert.IsType<BadRequestObjectResult>(result.Result);
        VerifyAuditLogNeverCalled();
    }

    [Fact]
    public async Task CompleteHandover_Valid_TransitionsToRegisteredAndRecordsHistory()
    {
        var visit = _context.AddVisit(status: VisitStatus.GateRegistered);
        var department = _context.AddDepartment();
        var host = _context.AddEmployee(departmentId: department.Id, position: "Officer");

        var result = await _controller.CompleteHandover(visit.Id, new CompleteHandoverDto { HostEmployeeId = host.Id, DepartmentId = department.Id, Purpose = VisitPurposeType.Official });

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var dto = Assert.IsType<VisitDto>(ok.Value);
        Assert.Equal(VisitStatus.Registered, dto.Status);
        Assert.Equal(host.Id, dto.HostEmployeeId);
        Assert.Equal(department.Id, dto.DepartmentId);

        var history = await ReloadHistoryAsync(visit.Id);
        var entry = Assert.Single(history);
        Assert.Equal(VisitStatus.GateRegistered, entry.FromStatus);
        Assert.Equal(VisitStatus.Registered, entry.ToStatus);
        VerifyAuditLogCalled(AuditAction.StatusChanged, Times.Once());
    }

    // ---- UpdateGateDetails ----

    [Fact]
    public async Task UpdateGateDetails_NotFound_ReturnsNotFound()
    {
        var result = await _controller.UpdateGateDetails(999, new UpdateGateDetailsDto());

        Assert.IsType<NotFoundResult>(result.Result);
    }

    [Theory]
    [InlineData(VisitStatus.Completed)]
    [InlineData(VisitStatus.Closed)]
    [InlineData(VisitStatus.Cancelled)]
    [InlineData(VisitStatus.Denied)]
    public async Task UpdateGateDetails_ClosedVisit_ReturnsConflictAndLeavesFieldsUnchanged(VisitStatus status)
    {
        var visit = _context.AddVisit(status: status);

        var result = await _controller.UpdateGateDetails(visit.Id, new UpdateGateDetailsDto { VehicleModel = "Toyota" });

        Assert.IsType<ConflictObjectResult>(result.Result);
        var reloaded = await ReloadVisitAsync(visit.Id);
        Assert.Null(reloaded!.VehicleModel);
    }

    [Fact]
    public async Task UpdateGateDetails_Valid_UpdatesFields()
    {
        var visit = _context.AddVisit(status: VisitStatus.GateRegistered);

        var result = await _controller.UpdateGateDetails(visit.Id, new UpdateGateDetailsDto { VehicleModel = "Toyota", VehiclePlateNumber = "KAA-1" });

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var dto = Assert.IsType<VisitDto>(ok.Value);
        Assert.Equal("Toyota", dto.VehicleModel);
        Assert.Equal("KAA-1", dto.VehiclePlateNumber);

        var reloaded = await ReloadVisitAsync(visit.Id);
        Assert.Equal("Toyota", reloaded!.VehicleModel);
        Assert.Equal(VisitStatus.GateRegistered, reloaded.Status);
    }

    // ---- NotifyHost ----

    [Fact]
    public async Task NotifyHost_WrongStatus_ReturnsConflictAndDoesNotRecordHistory()
    {
        var visit = _context.AddVisit(status: VisitStatus.GateRegistered);
        var actor = _context.AddUser();

        var result = await _controller.NotifyHost(visit.Id, new VisitNotifyHostDto { ChangedByUserId = actor.Id });

        Assert.IsType<ConflictObjectResult>(result.Result);
        var reloaded = await ReloadVisitAsync(visit.Id);
        Assert.Equal(VisitStatus.GateRegistered, reloaded!.Status);
        Assert.Empty(await ReloadHistoryAsync(visit.Id));
        VerifyAuditLogNeverCalled();
    }

    [Fact]
    public async Task NotifyHost_ActorMissing_ReturnsBadRequestAndLeavesVisitUnchanged()
    {
        var visit = _context.AddVisit(status: VisitStatus.Registered);

        var result = await _controller.NotifyHost(visit.Id, new VisitNotifyHostDto { ChangedByUserId = 999 });

        Assert.IsType<BadRequestObjectResult>(result.Result);
        var reloaded = await ReloadVisitAsync(visit.Id);
        Assert.Equal(VisitStatus.Registered, reloaded!.Status);
        Assert.Empty(await ReloadHistoryAsync(visit.Id));
        VerifyAuditLogNeverCalled();
    }

    [Fact]
    public async Task NotifyHost_Valid_TransitionsRecordsHistoryAndQueuesNotification()
    {
        var visit = _context.AddVisit(status: VisitStatus.Registered);
        var actor = _context.AddUser();

        var result = await _controller.NotifyHost(visit.Id, new VisitNotifyHostDto { ChangedByUserId = actor.Id, Remarks = "Waiting" });

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var dto = Assert.IsType<VisitDto>(ok.Value);
        Assert.Equal(VisitStatus.WaitingForHost, dto.Status);

        var history = await ReloadHistoryAsync(visit.Id);
        var entry = Assert.Single(history);
        Assert.Equal(VisitStatus.Registered, entry.FromStatus);
        Assert.Equal(VisitStatus.WaitingForHost, entry.ToStatus);
        Assert.Equal("Waiting", entry.Remarks);

        await using var verifyContext = _factory.CreateContext();
        var notification = Assert.Single(verifyContext.Notifications.Where(n => n.VisitId == visit.Id));
        Assert.Equal(NotificationType.VisitorArrived, notification.Type);
        VerifyAuditLogCalled(AuditAction.NotificationSent, Times.Once());
    }

    // ---- HostAcknowledge ----

    [Fact]
    public async Task HostAcknowledge_WrongStatus_ReturnsConflict()
    {
        var visit = _context.AddVisit(status: VisitStatus.Registered);
        var actor = _context.AddUser();

        var result = await _controller.HostAcknowledge(visit.Id, new VisitHostAcknowledgeDto { ChangedByUserId = actor.Id });

        Assert.IsType<ConflictObjectResult>(result.Result);
        var reloaded = await ReloadVisitAsync(visit.Id);
        Assert.Equal(VisitStatus.Registered, reloaded!.Status);
        Assert.Null(reloaded.HostAcknowledgedAt);
        VerifyAuditLogNeverCalled();
    }

    [Fact]
    public async Task HostAcknowledge_ActorMissing_ReturnsBadRequest()
    {
        var visit = _context.AddVisit(status: VisitStatus.WaitingForHost);

        var result = await _controller.HostAcknowledge(visit.Id, new VisitHostAcknowledgeDto { ChangedByUserId = 999 });

        Assert.IsType<BadRequestObjectResult>(result.Result);
        var reloaded = await ReloadVisitAsync(visit.Id);
        Assert.Equal(VisitStatus.WaitingForHost, reloaded!.Status);
    }

    [Fact]
    public async Task HostAcknowledge_Valid_SetsAcknowledgedAtAndRecordsHistory()
    {
        var visit = _context.AddVisit(status: VisitStatus.WaitingForHost);
        var actor = _context.AddUser();

        var result = await _controller.HostAcknowledge(visit.Id, new VisitHostAcknowledgeDto { ChangedByUserId = actor.Id, StaffAvailabilityStatus = StaffAvailabilityStatus.Available });

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var dto = Assert.IsType<VisitDto>(ok.Value);
        Assert.Equal(VisitStatus.HostAcknowledged, dto.Status);
        Assert.NotNull(dto.HostAcknowledgedAt);
        Assert.Equal(StaffAvailabilityStatus.Available, dto.StaffAvailabilityStatus);

        var history = await ReloadHistoryAsync(visit.Id);
        var entry = Assert.Single(history);
        Assert.Equal(VisitStatus.WaitingForHost, entry.FromStatus);
        Assert.Equal(VisitStatus.HostAcknowledged, entry.ToStatus);

        await using var verifyContext = _factory.CreateContext();
        Assert.Single(verifyContext.Notifications.Where(n => n.VisitId == visit.Id && n.Type == NotificationType.HostAcknowledged));
        VerifyAuditLogCalled(AuditAction.StatusChanged, Times.Once());
    }

    // ---- Deny ----

    [Fact]
    public async Task Deny_WrongStatus_ReturnsConflict()
    {
        var visit = _context.AddVisit(status: VisitStatus.Registered);
        var actor = _context.AddUser();

        var result = await _controller.Deny(visit.Id, new VisitDenyDto { ChangedByUserId = actor.Id });

        Assert.IsType<ConflictObjectResult>(result.Result);
        var reloaded = await ReloadVisitAsync(visit.Id);
        Assert.Equal(VisitStatus.Registered, reloaded!.Status);
        Assert.Null(reloaded.ClosedAt);
    }

    [Fact]
    public async Task Deny_Valid_ClosesVisitAndRecordsHistory()
    {
        var visit = _context.AddVisit(status: VisitStatus.WaitingForHost);
        var actor = _context.AddUser();

        var result = await _controller.Deny(visit.Id, new VisitDenyDto { ChangedByUserId = actor.Id, Remarks = "Not available" });

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var dto = Assert.IsType<VisitDto>(ok.Value);
        Assert.Equal(VisitStatus.Denied, dto.Status);
        Assert.NotNull(dto.ClosedAt);

        var history = await ReloadHistoryAsync(visit.Id);
        var entry = Assert.Single(history);
        Assert.Equal(VisitStatus.WaitingForHost, entry.FromStatus);
        Assert.Equal(VisitStatus.Denied, entry.ToStatus);
        Assert.Equal("Not available", entry.Remarks);
        VerifyAuditLogCalled(AuditAction.StatusChanged, Times.Once());
    }

    // ---- MarkAttended ----

    [Fact]
    public async Task MarkAttended_WrongStatus_ReturnsConflict()
    {
        var visit = _context.AddVisit(status: VisitStatus.WaitingForHost);
        var actor = _context.AddUser();

        var result = await _controller.MarkAttended(visit.Id, new VisitMarkAttendedDto { ChangedByUserId = actor.Id });

        Assert.IsType<ConflictObjectResult>(result.Result);
        var reloaded = await ReloadVisitAsync(visit.Id);
        Assert.Equal(VisitStatus.WaitingForHost, reloaded!.Status);
    }

    [Fact]
    public async Task MarkAttended_Valid_TransitionsToAttendedAndRecordsHistory()
    {
        var visit = _context.AddVisit(status: VisitStatus.HostAcknowledged);
        var actor = _context.AddUser();

        var result = await _controller.MarkAttended(visit.Id, new VisitMarkAttendedDto { ChangedByUserId = actor.Id });

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var dto = Assert.IsType<VisitDto>(ok.Value);
        Assert.Equal(VisitStatus.Attended, dto.Status);

        var history = await ReloadHistoryAsync(visit.Id);
        var entry = Assert.Single(history);
        Assert.Equal(VisitStatus.HostAcknowledged, entry.FromStatus);
        Assert.Equal(VisitStatus.Attended, entry.ToStatus);
        VerifyAuditLogCalled(AuditAction.StatusChanged, Times.Once());
    }

    // ---- HostComplete ----

    [Fact]
    public async Task HostComplete_WrongStatus_ReturnsConflict()
    {
        var visit = _context.AddVisit(status: VisitStatus.HostAcknowledged);
        var actor = _context.AddUser();

        var result = await _controller.HostComplete(visit.Id, new VisitHostCompleteDto { ChangedByUserId = actor.Id });

        Assert.IsType<ConflictObjectResult>(result.Result);
        var reloaded = await ReloadVisitAsync(visit.Id);
        Assert.Equal(VisitStatus.HostAcknowledged, reloaded!.Status);
        Assert.Null(reloaded.HostCompletedAt);
    }

    [Fact]
    public async Task HostComplete_Valid_TransitionsToAwaitingExitAndQueuesCheckoutNotification()
    {
        var visit = _context.AddVisit(status: VisitStatus.Attended);
        var actor = _context.AddUser();

        var result = await _controller.HostComplete(visit.Id, new VisitHostCompleteDto { ChangedByUserId = actor.Id });

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var dto = Assert.IsType<VisitDto>(ok.Value);
        Assert.Equal(VisitStatus.AwaitingExit, dto.Status);
        Assert.NotNull(dto.HostCompletedAt);

        var history = await ReloadHistoryAsync(visit.Id);
        var entry = Assert.Single(history);
        Assert.Equal(VisitStatus.Attended, entry.FromStatus);
        Assert.Equal(VisitStatus.AwaitingExit, entry.ToStatus);

        await using var verifyContext = _factory.CreateContext();
        Assert.Single(verifyContext.Notifications.Where(n => n.VisitId == visit.Id && n.Type == NotificationType.CheckoutRequired));
        VerifyAuditLogCalled(AuditAction.StatusChanged, Times.Once());
    }

    // ---- CheckOut ----

    [Fact]
    public async Task CheckOut_WrongStatus_ReturnsConflict()
    {
        var visit = _context.AddVisit(status: VisitStatus.Attended);
        var actor = _context.AddUser();

        var result = await _controller.CheckOut(visit.Id, new VisitCheckOutDto { CheckedOutById = actor.Id });

        Assert.IsType<ConflictObjectResult>(result.Result);
        var reloaded = await ReloadVisitAsync(visit.Id);
        Assert.Equal(VisitStatus.Attended, reloaded!.Status);
        Assert.Null(reloaded.DepartureTime);
    }

    [Fact]
    public async Task CheckOut_ActorMissing_ReturnsBadRequest()
    {
        var visit = _context.AddVisit(status: VisitStatus.AwaitingExit);

        var result = await _controller.CheckOut(visit.Id, new VisitCheckOutDto { CheckedOutById = 999 });

        Assert.IsType<BadRequestObjectResult>(result.Result);
        var reloaded = await ReloadVisitAsync(visit.Id);
        Assert.Equal(VisitStatus.AwaitingExit, reloaded!.Status);
    }

    [Fact]
    public async Task CheckOut_BadgeReturnedTrue_SetsBadgeAvailableAndLogsBothActions()
    {
        var visit = _context.AddVisit(status: VisitStatus.AwaitingExit, badgeNumber: "B-1");
        var actor = _context.AddUser();

        var result = await _controller.CheckOut(visit.Id, new VisitCheckOutDto { CheckedOutById = actor.Id, BadgeReturned = true });

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var dto = Assert.IsType<VisitDto>(ok.Value);
        Assert.Equal(VisitStatus.Completed, dto.Status);
        Assert.Equal(BadgeStatus.Available, dto.BadgeStatus);
        Assert.NotNull(dto.BadgeReturnedAt);
        Assert.NotNull(dto.DepartureTime);
        Assert.Equal(actor.Id, dto.CheckedOutById);

        VerifyAuditLogCalled(AuditAction.CheckOut, Times.Once());
        VerifyAuditLogCalled(AuditAction.BadgeReturned, Times.Once());
    }

    [Fact]
    public async Task CheckOut_NoBadgeOnVisit_LeavesBadgeReturnedAtNullAndSkipsBadgeReturnedLog()
    {
        var visit = _context.AddVisit(status: VisitStatus.AwaitingExit);
        var actor = _context.AddUser();

        var result = await _controller.CheckOut(visit.Id, new VisitCheckOutDto { CheckedOutById = actor.Id, BadgeReturned = true });

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var dto = Assert.IsType<VisitDto>(ok.Value);
        Assert.Null(dto.BadgeReturnedAt);
        Assert.Null(dto.BadgeStatus);

        VerifyAuditLogCalled(AuditAction.CheckOut, Times.Once());
        VerifyAuditLogCalled(AuditAction.BadgeReturned, Times.Never());
    }

    [Fact]
    public async Task CheckOut_BadgeReturnedFalse_LeavesIssuedBadgeUntouched()
    {
        var visit = _context.AddVisit(status: VisitStatus.AwaitingExit, badgeNumber: "B-1");
        var actor = _context.AddUser();

        var result = await _controller.CheckOut(visit.Id, new VisitCheckOutDto { CheckedOutById = actor.Id, BadgeReturned = false });

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var dto = Assert.IsType<VisitDto>(ok.Value);
        Assert.Equal(BadgeStatus.Issued, dto.BadgeStatus);
        Assert.Null(dto.BadgeReturnedAt);
        VerifyAuditLogCalled(AuditAction.BadgeReturned, Times.Never());
    }

    // ---- Close ----

    [Fact]
    public async Task Close_WrongStatus_ReturnsConflict()
    {
        var visit = _context.AddVisit(status: VisitStatus.AwaitingExit);
        var actor = _context.AddUser();

        var result = await _controller.Close(visit.Id, new VisitCloseDto { ChangedByUserId = actor.Id });

        Assert.IsType<ConflictObjectResult>(result.Result);
        var reloaded = await ReloadVisitAsync(visit.Id);
        Assert.Null(reloaded!.ClosedAt);
    }

    [Fact]
    public async Task Close_Valid_SetsClosedAtAndRecordsHistory()
    {
        var visit = _context.AddVisit(status: VisitStatus.Completed);
        var actor = _context.AddUser();

        var result = await _controller.Close(visit.Id, new VisitCloseDto { ChangedByUserId = actor.Id });

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var dto = Assert.IsType<VisitDto>(ok.Value);
        Assert.Equal(VisitStatus.Closed, dto.Status);
        Assert.NotNull(dto.ClosedAt);

        var history = await ReloadHistoryAsync(visit.Id);
        var entry = Assert.Single(history);
        Assert.Equal(VisitStatus.Completed, entry.FromStatus);
        Assert.Equal(VisitStatus.Closed, entry.ToStatus);
        VerifyAuditLogCalled(AuditAction.StatusChanged, Times.Once());
    }

    // ---- Cancel ----

    [Theory]
    [InlineData(VisitStatus.Registered)]
    [InlineData(VisitStatus.WaitingForHost)]
    [InlineData(VisitStatus.HostAcknowledged)]
    [InlineData(VisitStatus.Attended)]
    public async Task Cancel_CancellableStatus_SetsCancelledRecordsHistoryFromOriginalStatus(VisitStatus status)
    {
        var visit = _context.AddVisit(status: status);
        var actor = _context.AddUser();

        var result = await _controller.Cancel(visit.Id, new VisitCancelDto { ChangedByUserId = actor.Id });

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var dto = Assert.IsType<VisitDto>(ok.Value);
        Assert.Equal(VisitStatus.Cancelled, dto.Status);
        Assert.NotNull(dto.ClosedAt);

        var history = await ReloadHistoryAsync(visit.Id);
        var entry = Assert.Single(history);
        Assert.Equal(status, entry.FromStatus);
        Assert.Equal(VisitStatus.Cancelled, entry.ToStatus);
        VerifyAuditLogCalled(AuditAction.StatusChanged, Times.Once());
    }

    [Theory]
    [InlineData(VisitStatus.Completed)]
    [InlineData(VisitStatus.Closed)]
    [InlineData(VisitStatus.Cancelled)]
    [InlineData(VisitStatus.Denied)]
    [InlineData(VisitStatus.AwaitingExit)]
    [InlineData(VisitStatus.GateRegistered)]
    public async Task Cancel_NotCancellableStatus_ReturnsConflictAndLeavesStatusUnchanged(VisitStatus status)
    {
        var visit = _context.AddVisit(status: status);
        var actor = _context.AddUser();

        var result = await _controller.Cancel(visit.Id, new VisitCancelDto { ChangedByUserId = actor.Id });

        Assert.IsType<ConflictObjectResult>(result.Result);
        var reloaded = await ReloadVisitAsync(visit.Id);
        Assert.Equal(status, reloaded!.Status);
        Assert.Empty(await ReloadHistoryAsync(visit.Id));
        VerifyAuditLogNeverCalled();
    }

    // ---- GetAll / GetById / GetHistory ----

    [Fact]
    public async Task GetAll_NoFilter_ReturnsAllVisits()
    {
        _context.AddVisit(status: VisitStatus.GateRegistered);
        _context.AddVisit(status: VisitStatus.Registered);

        var result = await _controller.GetAll();

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var visits = Assert.IsAssignableFrom<IEnumerable<VisitDto>>(ok.Value);
        Assert.Equal(2, visits.Count());
    }

    [Fact]
    public async Task GetAll_FiltersByStatus_ReturnsOnlyMatching()
    {
        _context.AddVisit(status: VisitStatus.GateRegistered);
        var registered = _context.AddVisit(status: VisitStatus.Registered);

        var result = await _controller.GetAll(VisitStatus.Registered);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var visits = Assert.IsAssignableFrom<IEnumerable<VisitDto>>(ok.Value).ToList();
        var visit = Assert.Single(visits);
        Assert.Equal(registered.Id, visit.Id);
    }

    [Fact]
    public async Task GetById_NotFound_ReturnsNotFound()
    {
        var result = await _controller.GetById(999);

        Assert.IsType<NotFoundResult>(result.Result);
    }

    [Fact]
    public async Task GetById_Existing_ReturnsMatchingVisit()
    {
        var visit = _context.AddVisit(status: VisitStatus.Registered);

        var result = await _controller.GetById(visit.Id);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var dto = Assert.IsType<VisitDto>(ok.Value);
        Assert.Equal(visit.Id, dto.Id);
        Assert.Equal(VisitStatus.Registered, dto.Status);
    }

    [Fact]
    public async Task GetHistory_VisitMissing_ReturnsNotFound()
    {
        var result = await _controller.GetHistory(999);

        Assert.IsType<NotFoundResult>(result.Result);
    }

    [Fact]
    public async Task GetHistory_ReturnsEntriesInChronologicalOrderWithCorrectTransitions()
    {
        var visit = _context.AddVisit(status: VisitStatus.Registered);
        var actor = _context.AddUser();

        await _controller.NotifyHost(visit.Id, new VisitNotifyHostDto { ChangedByUserId = actor.Id });
        await _controller.HostAcknowledge(visit.Id, new VisitHostAcknowledgeDto { ChangedByUserId = actor.Id });

        var result = await _controller.GetHistory(visit.Id);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var entries = Assert.IsAssignableFrom<IEnumerable<VisitStatusHistoryDto>>(ok.Value).ToList();
        Assert.Equal(2, entries.Count);
        Assert.Equal(VisitStatus.Registered, entries[0].FromStatus);
        Assert.Equal(VisitStatus.WaitingForHost, entries[0].ToStatus);
        Assert.Equal(VisitStatus.WaitingForHost, entries[1].FromStatus);
        Assert.Equal(VisitStatus.HostAcknowledged, entries[1].ToStatus);
        Assert.True(entries[0].ChangedAt <= entries[1].ChangedAt);
    }
}
