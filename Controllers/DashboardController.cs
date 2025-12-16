using KarlixQMS.API.Data;
using KarlixQMS.API.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OpenIddict.Validation.AspNetCore;

namespace KarlixQMS.API.Controllers;

[ApiController]
[Route("api/dashboard")]
[Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)]
public class DashboardController : ControllerBase
{
    private readonly QmsDbContext _db;
    private readonly ITenantContext _tenant;

    public DashboardController(QmsDbContext db, ITenantContext tenant)
    {
        _db = db;
        _tenant = tenant;
    }

    [HttpGet]
    public async Task<IActionResult> Get()
    {
        var tenantId = _tenant.TenantId;

        // 1) Summary (tenant-aware) – iz vw_QmsIssueList (ima TenantId)
        var issueBase = _db.vw_QmsIssueLists
            .AsNoTracking()
            .Where(x => x.TenantId == tenantId);

        var totalIssues = await issueBase.CountAsync();
        var openIssues = await issueBase.CountAsync(x => x.StatusCode != null && x.StatusCode != "CLOSED" && x.StatusCode != "CANCELLED");
        var closedIssues = await issueBase.CountAsync(x => x.StatusCode == "CLOSED");
        var cancelledIssues = await issueBase.CountAsync(x => x.StatusCode == "CANCELLED");

        // 2) Actions summary (tenant-aware) – vw_QmsActionOverview (ima TenantId)
        var actionBase = _db.vw_QmsActionOverviews
            .AsNoTracking()
            .Where(x => x.TenantId == tenantId && x.IsActive == true);

        var totalActions = await actionBase.CountAsync();
        var completedActions = await actionBase.CountAsync(x => x.CompletedDate != null);
        var evaluatedActions = await actionBase.CountAsync(x => x.VerificationDate != null);

        // 3) Trend (tenant-aware) – vw_QmsIssueKpiMonthly (ima TenantId)
        var trend = await _db.vw_QmsIssueKpiMonthlies
            .AsNoTracking()
            .Where(x => x.TenantId == tenantId)
            .OrderByDescending(x => x.Year)
            .ThenByDescending(x => x.Month)
            .Take(12)
            .ToListAsync();

        // 4) Recent cases (tenant-aware) – vw_QmsIssueList
        var recent = await _db.vw_QmsIssueLists
            .AsNoTracking()
            .Where(x => x.TenantId == tenantId)
            .OrderByDescending(x => x.IssueDate)
            .Take(10)
            .Select(x => new
            {
                x.Number,
                x.EntityType,
                x.Title,
                x.StatusCode,
                x.StatusName,
                x.IssueDate
            })
            .ToListAsync();

        return Ok(new
        {
            TenantId = tenantId,
            Summary = new
            {
                TotalIssues = totalIssues,
                OpenIssues = openIssues,
                ClosedIssues = closedIssues,
                CancelledIssues = cancelledIssues,
                TotalActions = totalActions,
                CompletedActions = completedActions,
                EvaluatedActions = evaluatedActions
            },
            RecentCases = recent,
            MonthlyTrend = trend
        });
    }
}
