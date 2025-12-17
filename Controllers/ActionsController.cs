using KarlixQMS.API.Data;
using KarlixQMS.API.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OpenIddict.Validation.AspNetCore;

namespace KarlixQMS.API.Controllers;

[ApiController]
[Route("api/actions")]
[Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)]
public class ActionsController : ControllerBase
{
    private readonly QmsDbContext _db;
    private readonly ITenantContext _tenant;

    public ActionsController(QmsDbContext db, ITenantContext tenant)
    {
        _db = db;
        _tenant = tenant;
    }

    // LISTA (kao što već imaš)
    [HttpGet]
    public async Task<IActionResult> Get(
        [FromQuery] bool openOnly = true,
        [FromQuery] bool? overdue = null,
        [FromQuery] string? type = null,     // COMPLAINT / NONCONFORMITY
        [FromQuery] int take = 200)
    {
        var tenantId = _tenant.TenantId;
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        var q = _db.vw_QmsActionOverviews
            .AsNoTracking()
            .Where(x => x.TenantId == tenantId && x.IsActive == true);

        if (openOnly)
            q = q.Where(x => x.CompletedDate == null);

        if (!string.IsNullOrWhiteSpace(type))
            q = q.Where(x => x.EntityType == type);

        if (overdue.HasValue)
        {
            if (overdue.Value)
                q = q.Where(x => x.DueDate != null && x.DueDate < today);
            else
                q = q.Where(x => x.DueDate == null || x.DueDate >= today);
        }

        var items = await q
            .OrderBy(x => x.DueDate == null)
            .ThenBy(x => x.DueDate)
            .Take(Math.Clamp(take, 1, 1000))
            .Select(x => new
            {
                x.ActionId,
                x.IssueNumber,
                x.EntityType,
                x.ActionTitle,
                x.ActionTypeName,
                x.ResponsibleName,
                DueDate = x.DueDate.HasValue
                    ? x.DueDate.Value.ToDateTime(TimeOnly.MinValue)
                    : (DateTime?)null,
                StatusCode = (x.DueDate != null && x.DueDate < today) ? "OVERDUE" : "IN_PROGRESS",
                DaysLate = (x.DueDate != null && x.DueDate < today) ? (today.DayNumber - x.DueDate.Value.DayNumber) : (int?)null
            })
            .ToListAsync();

        return Ok(items);
    }

    // DETAILS
    // GET /api/actions/{id}
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById([FromRoute] Guid id)
    {
        var tenantId = _tenant.TenantId;

        // Primarno: vw_QmsActionList (ima sve što treba za UI)
        var row = await _db.vw_QmsActionLists
            .AsNoTracking()
            .Where(x => x.TenantId == tenantId && x.Id == id)
            .Select(x => new
            {
                ActionId = x.Id,
                x.Title,
                x.Description,

                // referenca na "entity" (RIN/UN)
                x.EntityType,
                x.EntityId,
                x.EntityNumber,
                x.EntityTitle,

                // rokovi / status
                DueDate = x.DueDate.HasValue ? x.DueDate.Value.ToDateTime(TimeOnly.MinValue) : (DateTime?)null,
                CompletedDate = x.CompletedDate.HasValue ? x.CompletedDate.Value.ToDateTime(TimeOnly.MinValue) : (DateTime?)null,

                // odgovorni / OU
                ResponsibleName = x.Responsible,
                x.OrgUnitCode,
                x.OrgUnitName,

                // tip / učinkovitost
                x.ActionTypeCode,
                x.ActionTypeName,
                x.EffectivenessCode,
                x.EffectivenessName
            })
            .FirstOrDefaultAsync();

        if (row == null)
            return NotFound(new { Message = $"Action '{id}' nije pronađen." });

        return Ok(row);
    }
}
