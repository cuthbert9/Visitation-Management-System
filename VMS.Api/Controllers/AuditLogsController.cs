using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VisitorManagementSystem.Api.Models;
using VisitorManagementSystem.Domain.Entities;
using VisitorManagementSystem.Domain.Enums;
using VisitorManagementSystem.Infrastructure.Data;

namespace VisitorManagementSystem.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuditLogsController : ControllerBase
{
    private readonly AppDbContext _context;

    public AuditLogsController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<AuditLogDto>>> GetAll(
        [FromQuery] string? entityType,
        [FromQuery] int? entityId,
        [FromQuery] int? userId,
        [FromQuery] AuditAction? action)
    {
        var query = _context.AuditLogs.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(entityType))
        {
            query = query.Where(log => log.EntityType == entityType);
        }

        if (entityId.HasValue)
        {
            query = query.Where(log => log.EntityId == entityId.Value);
        }

        if (userId.HasValue)
        {
            query = query.Where(log => log.UserId == userId.Value);
        }

        if (action.HasValue)
        {
            query = query.Where(log => log.Action == action.Value);
        }

        var logs = await query
            .OrderByDescending(log => log.CreatedAt)
            .Select(log => ToAuditLogDto(log))
            .ToListAsync();

        return Ok(logs);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<AuditLogDto>> GetById(int id)
    {
        var log = await _context.AuditLogs.AsNoTracking().FirstOrDefaultAsync(existingLog => existingLog.Id == id);
        return log is null ? NotFound() : Ok(ToAuditLogDto(log));
    }

    private static AuditLogDto ToAuditLogDto(AuditLog log) => new()
    {
        Id = log.Id,
        UserId = log.UserId,
        ActorType = log.ActorType,
        Action = log.Action,
        EntityType = log.EntityType,
        EntityId = log.EntityId,
        OldValue = log.OldValue,
        NewValue = log.NewValue,
        Description = log.Description,
        IpAddress = log.IpAddress,
        UserAgent = log.UserAgent,
        CorrelationId = log.CorrelationId,
        CreatedAt = log.CreatedAt
    };
}
