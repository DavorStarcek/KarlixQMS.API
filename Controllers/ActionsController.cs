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

    // LISTA (dashboard/open actions)
    // GET /api/actions?openOnly=true&overdue=true&type=COMPLAINT&caseNumber=RIN-1234
    [HttpGet]
    public async Task<IActionResult> Get(
        [FromQuery] bool openOnly = true,
        [FromQuery] bool? overdue = null,
        [FromQuery] string? type = null,        // COMPLAINT / NONCONFORMITY
        [FromQuery] string? caseNumber = null,  // IssueNumber (RIN-xxxx ili broj za UN)
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

        // filter po broju slučaja (IssueNumber)
        if (!string.IsNullOrWhiteSpace(caseNumber))
        {
            var pattern = $"%{caseNumber.Trim()}%";
            q = q.Where(x => x.IssueNumber != null && EF.Functions.Like(x.IssueNumber, pattern));
        }

        if (overdue.HasValue)
        {
            if (overdue.Value)
                q = q.Where(x => x.DueDate != null && x.DueDate < today);
            else
                q = q.Where(x => x.DueDate == null || x.DueDate >= today);
        }

        var items = await q
            .OrderBy(x => x.DueDate == null) // non-null prvo
            .ThenBy(x => x.DueDate)
            .Take(Math.Clamp(take, 1, 1000))
            .Select(x => new
            {
                ActionId = x.ActionId, // Guid

                x.IssueNumber,
                x.EntityType,
                x.ActionTitle,
                x.ActionTypeName,
                x.ResponsibleName,

                DueDate = x.DueDate.HasValue
                    ? x.DueDate.Value.ToDateTime(TimeOnly.MinValue)
                    : (DateTime?)null,

                CompletedDate = x.CompletedDate.HasValue
                    ? x.CompletedDate.Value.ToDateTime(TimeOnly.MinValue)
                    : (DateTime?)null,

                StatusCode = (x.CompletedDate == null && x.DueDate != null && x.DueDate < today)
                    ? "OVERDUE"
                    : "IN_PROGRESS",

                DaysLate = (x.CompletedDate == null && x.DueDate != null && x.DueDate < today)
                    ? (today.DayNumber - x.DueDate.Value.DayNumber)
                    : (int?)null
            })
            .ToListAsync();

        return Ok(items);
    }

    // DETAILS
    // GET /api/actions/{id}
    // id = QmsIssueAction.Id
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById([FromRoute] Guid id)
    {
        var tenantId = _tenant.TenantId;

        var row = await _db.vw_QmsIssue_Actions
            .AsNoTracking()
            .Where(x => x.TenantId == tenantId && x.Id == id)
            .Select(x => new
            {
                ActionId = x.Id,

                Title = x.ActionTitle,
                Description = x.ActionDescription,

                EntityType = x.IssueKind,      // COMPLAINT / NONCONFORMITY
                EntityNumber = x.IssueNumber,  // RIN-xxxx / UN-xxxx
                EntityTitle = x.IssueTitle,

                DueDate = x.DueDate.HasValue
                    ? x.DueDate.Value.ToDateTime(TimeOnly.MinValue)
                    : (DateTime?)null,

                CompletedDate = x.CompletedDate.HasValue
                    ? x.CompletedDate.Value.ToDateTime(TimeOnly.MinValue)
                    : (DateTime?)null,

                ResponsibleName = x.ResponsibleName,

                OrgUnitCode = x.ResponsibleOrgUnitCode,
                OrgUnitName = x.ResponsibleOrgUnitName,

                ActionTypeCode = x.ActionTypeCode,
                ActionTypeName = x.ActionTypeName,

                EffectivenessCode = x.EffectivenessCode,
                EffectivenessName = x.EffectivenessName,

                VerificationDate = x.VerificationDate.HasValue
                    ? x.VerificationDate.Value.ToDateTime(TimeOnly.MinValue)
                    : (DateTime?)null,

                VerificationNotes = x.VerificationNotes,

                CreatedAt = x.CreatedAt,
                UpdatedAt = x.UpdatedAt,

                IsDeleted = x.IsDeleted
            })
            .FirstOrDefaultAsync();

        if (row == null)
            return NotFound(new { Message = $"Action '{id}' nije pronađena." });

        return Ok(row);
    }

}
