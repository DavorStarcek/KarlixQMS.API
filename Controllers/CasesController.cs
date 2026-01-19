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

    // GET /api/cases?type=COMPLAINT&status=RECEIVED&statusGroup=open&q=RIN-123&take=200
    [HttpGet]
    public async Task<IActionResult> Get(
        [FromQuery] string? type = null,              // COMPLAINT / NONCONFORMITY
        [FromQuery] string? status = null,            // STATUS CODE (npr. RECEIVED, CLOSED_UNJUSTIFIED...)
        [FromQuery] string? statusGroup = null,       // open / final / cancelled
        [FromQuery] string? q = null,                 // search
        [FromQuery] int take = 200)
    {
        var tenantId = _tenant.TenantId;

        type = string.IsNullOrWhiteSpace(type) ? null : type.Trim();
        status = string.IsNullOrWhiteSpace(status) ? null : status.Trim();
        statusGroup = string.IsNullOrWhiteSpace(statusGroup) ? null : statusGroup.Trim().ToLowerInvariant();
        q = string.IsNullOrWhiteSpace(q) ? null : q.Trim();
        take = Math.Clamp(take, 1, 1000);

        // baza: view
        var issues = _db.vw_QmsIssueLists
            .AsNoTracking()
            .Where(x => x.TenantId == tenantId);

        if (!string.IsNullOrWhiteSpace(type))
            issues = issues.Where(x => x.EntityType == type);

        if (!string.IsNullOrWhiteSpace(q))
        {
            var pattern = $"%{q}%";
            issues = issues.Where(x =>
                EF.Functions.Like(x.Number, pattern) ||
                (x.Title != null && EF.Functions.Like(x.Title, pattern)) ||
                (x.CustomerName != null && EF.Functions.Like(x.CustomerName, pattern)));
        }

        // 1) TOČAN STATUS CODE (ako je poslan, ima prednost)
        if (!string.IsNullOrWhiteSpace(status))
        {
            issues = issues.Where(x => x.StatusCode == status);
        }
        // 2) STATUS GROUP (semantika)
        else if (!string.IsNullOrWhiteSpace(statusGroup))
        {
            // Dohvati skup WorkflowStatusId za ovaj tenant (+ optional type) koji spada u grupu
            var wsQuery = _db.QmsWorkflowStatuses
                .AsNoTracking()
                .Where(x => x.TenantId == tenantId && x.IsActive == true);

            if (!string.IsNullOrWhiteSpace(type))
                wsQuery = wsQuery.Where(x => x.EntityType == type);

            wsQuery = statusGroup switch
            {
                "open" => wsQuery.Where(x => x.IsFinal == false && x.IsCancelled == false),
                "final" => wsQuery.Where(x => x.IsFinal == true && x.IsCancelled == false),
                "cancelled" => wsQuery.Where(x => x.IsCancelled == true),
                _ => wsQuery.Where(x => true)
            };

            var wsIds = await wsQuery.Select(x => x.Id).ToListAsync();
            issues = issues.Where(x => wsIds.Contains(x.WorkflowStatusId));
        }

        // sort: newest first (IssueDate je DateOnly u view modelu)
        var items = await issues
            .OrderByDescending(x => x.IssueDate)
            .ThenByDescending(x => x.Number)
            .Take(take)
            .Select(x => new
            {
                x.EntityId,
                x.Number,
                x.EntityType,
                x.Title,
                IssueDate = x.IssueDate.ToDateTime(TimeOnly.MinValue),
                ReceivedDate = x.ReceivedDate.HasValue ? x.ReceivedDate.Value.ToDateTime(TimeOnly.MinValue) : (DateTime?)null,
                x.CustomerName,
                x.WorkflowStatusId,
                x.StatusCode,
                x.StatusName
            })
            .ToListAsync();

        return Ok(items);
    }

    // GET /api/cases/{number}
    // (ako već imaš svoj, ostavi ga; ovdje ga ne diram)
}
