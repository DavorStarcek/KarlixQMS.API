using KarlixQMS.API.Data;
using KarlixQMS.API.Infrastructure;
using KarlixQMS.API.Infrastructure.Security;
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

    // ============================================================
    // LIST
    // GET /api/cases?type=COMPLAINT&status=RECEIVED&q=RIN-12&statusGroup=OPEN&take=200
    // ============================================================
    [HttpGet]
    [Authorize(Policy = QmsPolicies.CasesRead)]
    public async Task<IActionResult> Get(
        [FromQuery] string? type = null,         // COMPLAINT / NONCONFORMITY
        [FromQuery] string? status = null,       // workflow status code (npr. RECEIVED)
        [FromQuery] string? statusGroup = null,  // OPEN / FINAL / CANCELLED
        [FromQuery] string? q = null,            // search (number/title/customer)
        [FromQuery] int take = 200)
    {
        var tenantId = _tenant.TenantId;

        type = string.IsNullOrWhiteSpace(type) ? null : type.Trim();
        status = string.IsNullOrWhiteSpace(status) ? null : status.Trim();
        statusGroup = string.IsNullOrWhiteSpace(statusGroup) ? null : statusGroup.Trim().ToUpperInvariant();
        q = string.IsNullOrWhiteSpace(q) ? null : q.Trim();
        take = Math.Clamp(take, 1, 1000);

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

        if (!string.IsNullOrWhiteSpace(status))
        {
            var code = status.ToUpperInvariant();

            issues = issues.Where(i =>
                _db.QmsWorkflowStatuses.Any(ws =>
                    ws.TenantId == tenantId &&
                    ws.IsActive == true &&
                    ws.Id == i.WorkflowStatusId &&
                    ws.Code != null &&
                    ws.Code.ToUpper() == code &&
                    (type == null || ws.EntityType == type)));
        }
        else if (!string.IsNullOrWhiteSpace(statusGroup))
        {
            issues = issues.Where(i =>
                _db.QmsWorkflowStatuses.Any(ws =>
                    ws.TenantId == tenantId &&
                    ws.IsActive == true &&
                    ws.Id == i.WorkflowStatusId &&
                    (type == null || ws.EntityType == type) &&
                    (
                        (statusGroup == "OPEN" && ws.IsFinal == false && ws.IsCancelled == false) ||
                        (statusGroup == "FINAL" && ws.IsFinal == true) ||
                        (statusGroup == "CANCELLED" && ws.IsCancelled == true)
                    )));
        }

        var items = await issues
            .OrderByDescending(x => x.IssueDate)
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

    // ============================================================
    // DETAILS (by number)
    // GET /api/cases/{number}
    // ============================================================
    [HttpGet("{number}")]
    [Authorize(Policy = QmsPolicies.CasesRead)]
    public async Task<IActionResult> GetByNumber([FromRoute] string number)
    {
        if (string.IsNullOrWhiteSpace(number))
            return BadRequest(new { Message = "Number is required." });

        var tenantId = _tenant.TenantId;
        number = number.Trim();

        var header = await _db.vw_QmsIssueLists
            .AsNoTracking()
            .Where(x => x.TenantId == tenantId && x.Number == number)
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
            .FirstOrDefaultAsync();

        if (header == null)
            return NotFound(new { Message = $"Case '{number}' nije pronađen." });

        // ✅ NOVO: izračun faze iz WorkflowStatus-a
        var ws = await _db.QmsWorkflowStatuses
            .AsNoTracking()
            .Where(x => x.TenantId == tenantId && x.IsActive == true && x.Id == header.WorkflowStatusId)
            .Select(x => new { x.IsInitial, x.IsFinal, x.IsCancelled, x.Code })
            .FirstOrDefaultAsync();

        var phase =
            ws?.IsFinal == true ? "CLOSED" :
            ws?.IsInitial == true ? "RECEIVED" :
            "IN_PROGRESS";

        var actions = await _db.vw_QmsIssue_Actions
            .AsNoTracking()
            .Where(a => a.TenantId == tenantId && a.IssueNumber == number)
            .OrderBy(a => a.DueDate == null)
            .ThenBy(a => a.DueDate)
            .ThenBy(a => a.ActionTitle)
            .Select(a => new
            {
                ActionId = a.Id,
                a.ActionTitle,
                a.ActionTypeName,
                a.ResponsibleName,
                DueDate = a.DueDate.HasValue ? a.DueDate.Value.ToDateTime(TimeOnly.MinValue) : (DateTime?)null,
                CompletedDate = a.CompletedDate.HasValue ? a.CompletedDate.Value.ToDateTime(TimeOnly.MinValue) : (DateTime?)null,
                VerificationDate = a.VerificationDate.HasValue ? a.VerificationDate.Value.ToDateTime(TimeOnly.MinValue) : (DateTime?)null,
                a.EffectivenessCode,
                a.EffectivenessName,
                a.IsDeleted
            })
            .ToListAsync();

        var entityType = (header.EntityType ?? "").Trim().ToUpperInvariant();

        object? complaint = null;
        object? nonconformity = null;

        if (entityType == "COMPLAINT")
        {
            complaint = await _db.QmsCustomerComplaints
                .AsNoTracking()
                .Where(x => x.TenantId == tenantId && x.Id == header.EntityId)
                .Select(x => new
                {
                    x.Id,
                    x.Number,

                    x.Title,
                    x.Description,
                    x.Analysis,
                    x.RootCause,
                    x.ImmediateCorrection,

                    x.IsComplaintJustified,
                    x.FeedbackToCustomer,

                    x.CustomerName,
                    x.ProductName,

                    x.OrgUnitId,

                    ClosedDate = x.ClosedDate.HasValue ? x.ClosedDate.Value.ToDateTime(TimeOnly.MinValue) : (DateTime?)null
                })
                .FirstOrDefaultAsync();
        }
        else if (entityType == "NONCONFORMITY")
        {
            nonconformity = await _db.QmsNonconformities
                .AsNoTracking()
                .Where(x => x.TenantId == tenantId && x.Id == header.EntityId)
                .Select(x => new
                {
                    x.Id,
                    x.Number,

                    x.Title,
                    x.Description,

                    x.RootCause,
                    x.Cause,
                    x.ImmediateCorrection,
                    x.CorrectiveAction,
                    x.PreventiveAction,

                    x.OrgUnitId,

                    DueDate = x.DueDate.HasValue ? x.DueDate.Value.ToDateTime(TimeOnly.MinValue) : (DateTime?)null,
                    CloseDate = x.CloseDate.HasValue ? x.CloseDate.Value.ToDateTime(TimeOnly.MinValue) : (DateTime?)null,

                    VerificationDate = x.VerificationDate.HasValue ? x.VerificationDate.Value.ToDateTime(TimeOnly.MinValue) : (DateTime?)null,
                    x.VerifiedBy,

                    x.EffectivenessId,
                    x.EffectivenessComment
                })
                .FirstOrDefaultAsync();
        }

        return Ok(new
        {
            header.EntityId,
            header.Number,
            header.EntityType,
            header.Title,
            header.IssueDate,
            header.ReceivedDate,
            header.CustomerName,
            header.WorkflowStatusId,
            header.StatusCode,
            header.StatusName,

            // ✅ NOVO
            Phase = phase,

            Actions = actions,

            Complaint = complaint,
            Nonconformity = nonconformity
        });
    }
}
