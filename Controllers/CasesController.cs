using KarlixQMS.API.Data;
using KarlixQMS.API.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OpenIddict.Validation.AspNetCore;

namespace KarlixQMS.API.Controllers;

[ApiController]
[Route("api/cases")]
[Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)]
public class CasesController : ControllerBase
{
    private readonly QmsDbContext _db;
    private readonly ITenantContext _tenant;

    public CasesController(QmsDbContext db, ITenantContext tenant)
    {
        _db = db;
        _tenant = tenant;
    }

    // LISTA
    [HttpGet]
    public async Task<IActionResult> Get(
        [FromQuery] string? type = null,        // COMPLAINT / NONCONFORMITY
        [FromQuery] string? status = null,      // open / closed / cancelled
        [FromQuery] string? number = null,      // LIKE pretraga po broju
        [FromQuery] int? year = null,
        [FromQuery] int? month = null,
        [FromQuery] int? lastDays = null,       // npr. 30
        [FromQuery] int take = 200)
    {
        var tenantId = _tenant.TenantId;

        var q = _db.vw_QmsIssueLists
            .AsNoTracking()
            .Where(x => x.TenantId == tenantId);

        if (!string.IsNullOrWhiteSpace(type))
            q = q.Where(x => x.EntityType == type);

        if (!string.IsNullOrWhiteSpace(number))
        {
            var pattern = $"%{number.Trim()}%";
            q = q.Where(x => EF.Functions.Like(x.Number, pattern));
        }

        if (!string.IsNullOrWhiteSpace(status))
        {
            switch (status.Trim().ToLowerInvariant())
            {
                case "open":
                    q = q.Where(x => x.StatusCode != null
                                     && x.StatusCode != "CLOSED"
                                     && x.StatusCode != "CANCELLED");
                    break;
                case "closed":
                    q = q.Where(x => x.StatusCode == "CLOSED");
                    break;
                case "cancelled":
                case "canceled":
                    q = q.Where(x => x.StatusCode == "CANCELLED");
                    break;
            }
        }

        if (year.HasValue && month.HasValue && month.Value is >= 1 and <= 12)
        {
            var from = new DateOnly(year.Value, month.Value, 1);
            var to = from.AddMonths(1);
            q = q.Where(x => x.IssueDate >= from && x.IssueDate < to);
        }

        if (lastDays.HasValue && lastDays.Value > 0)
        {
            var from = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-lastDays.Value));
            q = q.Where(x => x.IssueDate >= from);
        }

        var items = await q
            .OrderByDescending(x => x.IssueDate)
            .Take(Math.Clamp(take, 1, 1000))
            .Select(x => new
            {
                x.Number,
                x.EntityType,
                x.Title,
                x.StatusCode,
                x.StatusName,
                IssueDate = x.IssueDate.ToDateTime(TimeOnly.MinValue)
            })
            .ToListAsync();

        return Ok(items);
    }

    // DETAILS (HEADER)
    [HttpGet("{number}")]
    public async Task<IActionResult> GetByNumber([FromRoute] string number)
    {
        var tenantId = _tenant.TenantId;

        var item = await _db.vw_QmsIssueLists
            .AsNoTracking()
            .Where(x => x.TenantId == tenantId && x.Number == number)
            .Select(x => new
            {
                x.Number,
                x.EntityType,
                x.Title,
                x.StatusCode,
                x.StatusName,
                IssueDate = x.IssueDate.ToDateTime(TimeOnly.MinValue),
                x.CustomerName
            })
            .FirstOrDefaultAsync();

        if (item == null)
            return NotFound(new { Message = $"Case '{number}' nije pronađen." });

        return Ok(item);
    }

    // DETAILS (ACTIONS)
    // GET /api/cases/{number}/actions
    [HttpGet("{number}/actions")]
    public async Task<IActionResult> GetActions([FromRoute] string number)
    {
        var tenantId = _tenant.TenantId;

        // vw_QmsIssue_Actions sadrži: Id, IssueNumber, IssueKind, ActionTitle, DueDate, CompletedDate...
        var rows = await _db.vw_QmsIssue_Actions
            .AsNoTracking()
            .Where(x => x.TenantId == tenantId && x.IssueNumber == number)
            .OrderByDescending(x => x.UpdatedAt ?? x.CreatedAt)
            .Select(x => new
            {
                ActionId = x.Id,                 // <-- ključna promjena: Id = ActionId
                x.IssueKind,
                x.IssueNumber,
                x.IssueTitle,

                x.ActionTitle,
                x.ActionDescription,
                x.ActionTypeCode,
                x.ActionTypeName,

                x.ResponsibleName,
                x.ResponsibleOrgUnitCode,
                x.ResponsibleOrgUnitName,

                x.DueDate,
                x.CompletedDate,

                x.EffectivenessCode,
                x.EffectivenessName,

                x.VerificationDate,
                x.VerificationNotes,

                x.CreatedAt,
                x.UpdatedAt,
                x.IsDeleted
            })
            .ToListAsync();

        return Ok(rows);
    }


}
