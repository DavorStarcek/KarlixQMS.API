using KarlixQMS.API.Data;
using KarlixQMS.API.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OpenIddict.Validation.AspNetCore;

namespace KarlixQMS.API.Controllers;

[ApiController]
[Route("api/lookups")]
[Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)]
public class LookupsController : ControllerBase
{
    private readonly QmsDbContext _db;
    private readonly ITenantContext _tenant;

    public LookupsController(QmsDbContext db, ITenantContext tenant)
    {
        _db = db;
        _tenant = tenant;
    }

    // zajednički helper da svi lookupi budu konzistentni
    private Guid TenantId => _tenant.TenantId;

    [HttpGet("action-types")]
    public async Task<IActionResult> GetActionTypes()
    {
        var items = await _db.QmsActionTypes
            .AsNoTracking()
            .Where(x => x.TenantId == TenantId && x.IsActive == true)
            .OrderBy(x => x.DisplayOrder)
            .ThenBy(x => x.Name)
            .Select(x => new
            {
                x.Id,
                x.Code,
                x.Name
            })
            .ToListAsync();

        return Ok(items);
    }

    [HttpGet("effectiveness")]
    public async Task<IActionResult> GetEffectiveness()
    {
        var items = await _db.QmsEffectivenesses
            .AsNoTracking()
            .Where(x => x.TenantId == TenantId && x.IsActive == true)
            .OrderBy(x => x.DisplayOrder)
            .ThenBy(x => x.Name)
            .Select(x => new
            {
                x.Id,
                x.Code,
                x.Name
            })
            .ToListAsync();

        return Ok(items);
    }

    [HttpGet("org-units")]
    public async Task<IActionResult> GetOrgUnits()
    {
        var items = await _db.QmsOrgUnits
            .AsNoTracking()
            .Where(x => x.TenantId == TenantId && x.IsActive == true)
            .OrderBy(x => x.DisplayOrder)
            .ThenBy(x => x.Name)
            .Select(x => new
            {
                x.Id,
                x.Code,
                x.Name
            })
            .ToListAsync();

        return Ok(items);
    }

    // GET /api/lookups/workflow-statuses?entityType=COMPLAINT
    [HttpGet("workflow-statuses")]
    public async Task<IActionResult> GetWorkflowStatuses([FromQuery] string? entityType = null)
    {
        entityType = string.IsNullOrWhiteSpace(entityType) ? null : entityType.Trim();

        var q = _db.QmsWorkflowStatuses
            .AsNoTracking()
            .Where(x => x.TenantId == TenantId && x.IsActive == true);

        if (!string.IsNullOrWhiteSpace(entityType))
            q = q.Where(x => x.EntityType == entityType);

        var items = await q
            .OrderBy(x => x.EntityType)
            .ThenBy(x => x.DisplayOrder)
            .ThenBy(x => x.Name)
            .Select(x => new
            {
                x.Id,
                x.EntityType,
                x.Code,
                x.Name,
                x.Description,
                x.DisplayOrder,

                // korisno za UI (badgeovi, filteri “final/cancelled”)
                x.IsInitial,
                x.IsFinal,
                x.IsCancelled,
                x.IsActive
            })
            .ToListAsync();

        return Ok(items);
    }
}
