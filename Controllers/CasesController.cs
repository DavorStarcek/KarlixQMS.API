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

    [HttpGet]
    public async Task<IActionResult> Get(
        [FromQuery] string? type = null,        // COMPLAINT / NONCONFORMITY
        [FromQuery] string? status = null,      // open / closed / cancelled
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
}
