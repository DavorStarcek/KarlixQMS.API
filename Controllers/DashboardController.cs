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

        // 1) Issues base (tenant-aware) – vw_QmsIssueList
        var issueBase = _db.vw_QmsIssueLists
            .AsNoTracking()
            .Where(x => x.TenantId == tenantId);

        var statusCounts = await issueBase
        .GroupBy(x => x.StatusCode)
        .Select(g => new { StatusCode = g.Key, Count = g.Count() })
        .ToListAsync();

        var totalIssues = statusCounts.Sum(x => x.Count);
        var closedIssues = statusCounts.FirstOrDefault(x => x.StatusCode == "CLOSED")?.Count ?? 0;
        var cancelledIssues = statusCounts.FirstOrDefault(x => x.StatusCode == "CANCELLED")?.Count ?? 0;

        var openIssues = totalIssues - closedIssues - cancelledIssues; // ako ti je to definicija “open”


        // Pravi "zadnjih 30 dana" (IssueDate je DateOnly/DateOnly?)
        var from = DateOnly.FromDateTime(DateTime.UtcNow.Date.AddDays(-30));

        var complaintsLast30Days = await issueBase.CountAsync(x =>
            x.EntityType == "COMPLAINT" &&
            x.IssueDate >= from);

        var nonconformitiesLast30Days = await issueBase.CountAsync(x =>
            x.EntityType == "NONCONFORMITY" &&
            x.IssueDate >= from);

        // 2) Actions summary (tenant-aware) – vw_QmsActionOverview
        var actionBase = _db.vw_QmsActionOverviews
            .AsNoTracking()
            .Where(x => x.TenantId == tenantId && x.IsActive == true);

        var totalActions = await actionBase.CountAsync();
        var completedActions = await actionBase.CountAsync(x => x.CompletedDate != null);
        var evaluatedActions = await actionBase.CountAsync(x => x.VerificationDate != null);

        // 3) Trend – vw_QmsIssueKpiMonthly (zadnjih 12)
        var trend = await _db.vw_QmsIssueKpiMonthlies
            .AsNoTracking()
            .Where(x => x.TenantId == tenantId)
            .OrderByDescending(x => x.Year)
            .ThenByDescending(x => x.Month)
            .Take(12)
            .ToListAsync();

        // 4) Recent cases – vw_QmsIssueList (TOP 10)
        //    Web ti očekuje DateTime?, pa pretvaramo DateOnly -> DateTime u Select.
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
                IssueDate = ((DateOnly?)x.IssueDate).HasValue
                    ? ((DateOnly?)x.IssueDate)!.Value.ToDateTime(TimeOnly.MinValue)
                    : (DateTime?)null
            })
            .ToListAsync();

        // 5) Open actions – vw_QmsActionOverview (TOP 10)
        //    U Select uzmi DueDate kao DateOnly? pa ga kasnije pretvori u DateTime? za Web.
        var openActionsRaw = await _db.vw_QmsActionOverviews
            .AsNoTracking()
            .Where(x => x.TenantId == tenantId && x.IsActive == true)
            .Where(x => x.CompletedDate == null)
            .OrderBy(x => x.DueDate == null)   // false(0) prije true(1) → non-null prije null
            .ThenBy(x => x.DueDate)
            .Take(10)
            .Select(x => new
            {
                x.ActionId,
                x.IssueNumber,
                x.EntityType,
                x.ActionTitle,
                x.ActionTypeName,
                x.ResponsibleName,
                DueDate = (DateOnly?)x.DueDate
            })
            .ToListAsync();

        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        var openActions = openActionsRaw.Select(x =>
        {
            var status = "IN_PROGRESS";
            int? daysLate = null;

            if (x.DueDate.HasValue && x.DueDate.Value < today)
            {
                status = "OVERDUE";
                daysLate = today.DayNumber - x.DueDate.Value.DayNumber;
            }

            return new
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
                StatusCode = status,
                DaysLate = daysLate
            };
        }).ToList();

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
                EvaluatedActions = evaluatedActions,

                ComplaintsLast30Days = complaintsLast30Days,
                NonconformitiesLast30Days = nonconformitiesLast30Days
            },
            RecentCases = recent,
            MonthlyTrend = trend,
            OpenActions = openActions
        });
    }
}
