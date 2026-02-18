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

    // ----------------------------
    // DTOs
    // ----------------------------
    public sealed class CaseTransitionDto
    {
        public string? ToStatusCode { get; set; }
    }

    // ----------------------------
    // helpers
    // ----------------------------
    private static string PhaseFromFlags(bool isInitial, bool isFinal, bool isCancelled)
    {
        if (isCancelled) return "CANCELLED";
        if (isFinal) return "CLOSED";
        if (isInitial) return "RECEIVED";
        return "IN_PROGRESS";
    }

    // ============================================================
    // LIST
    // GET /api/cases?type=COMPLAINT&status=RECEIVED&q=RIN-12&statusGroup=OPEN&take=200
    // ============================================================
    [HttpGet]
    [Authorize(Policy = QmsPolicies.CasesRead)]
    public async Task<IActionResult> Get(
        [FromQuery] string? type = null,
        [FromQuery] string? status = null,
        [FromQuery] string? statusGroup = null,
        [FromQuery] string? q = null,
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

        var ws = await _db.QmsWorkflowStatuses
            .AsNoTracking()
            .Where(x => x.TenantId == tenantId && x.IsActive == true && x.Id == header.WorkflowStatusId)
            .Select(x => new { x.IsInitial, x.IsFinal, x.IsCancelled })
            .FirstOrDefaultAsync();

        var phase = ws == null ? "IN_PROGRESS" : PhaseFromFlags(ws.IsInitial, ws.IsFinal, ws.IsCancelled);

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

            Phase = phase,

            Actions = actions,

            Complaint = complaint,
            Nonconformity = nonconformity
        });
    }

    // ============================================================
    // HISTORY
    // GET /api/cases/{number}/history?take=200&phase=CLOSED
    // ============================================================
    [HttpGet("{number}/history")]
    [Authorize(Policy = QmsPolicies.CasesRead)]
    public async Task<IActionResult> GetHistory(
        [FromRoute] string number,
        [FromQuery] int take = 200,
        [FromQuery] string? phase = null)
    {
        if (string.IsNullOrWhiteSpace(number))
            return BadRequest(new { Message = "Number is required." });

        var tenantId = _tenant.TenantId;
        number = number.Trim();
        take = Math.Clamp(take, 1, 1000);
        phase = string.IsNullOrWhiteSpace(phase) ? null : phase.Trim().ToUpperInvariant();

        // header: EntityId = complaint/nonconformity id
        var header = await _db.vw_QmsIssueLists
            .AsNoTracking()
            .Where(x => x.TenantId == tenantId && x.Number == number)
            .Select(x => new { x.EntityType, x.EntityId })
            .FirstOrDefaultAsync();

        if (header == null)
            return NotFound(new { Message = $"Case '{number}' nije pronađen." });

        var entityType = (header.EntityType ?? "").Trim().ToUpperInvariant();

        // issueId (QmsIssue) preko ComplaintId/NonconformityId
        var issueQuery = _db.QmsIssues
            .AsNoTracking()
            .Where(x => x.TenantId == tenantId && x.EntityType == entityType);

        if (entityType == "COMPLAINT")
            issueQuery = issueQuery.Where(x => x.ComplaintId == header.EntityId);
        else
            issueQuery = issueQuery.Where(x => x.NonconformityId == header.EntityId);

        var issueId = await issueQuery.Select(x => x.Id).FirstOrDefaultAsync();

        if (issueId == Guid.Empty)
            return Ok(new List<object>());

        var q = _db.QmsIssueHistories
            .AsNoTracking()
            .Where(x => x.TenantId == tenantId && x.IssueId == issueId);

        if (!string.IsNullOrWhiteSpace(phase))
            q = q.Where(x => x.Phase != null && x.Phase.ToUpper() == phase);

        var items = await q
            .OrderByDescending(x => x.ChangedAt)
            .Take(take)
            .Select(x => new
            {
                x.Id,
                x.FromStatusCode,
                x.ToStatusCode,
                x.Phase,
                x.ChangedAt,          // UTC
                x.ChangedByUserId,
                x.ChangedByName
            })
            .ToListAsync();

        return Ok(items);
    }

    // ============================================================
    // WORKFLOW TRANSITION
    // POST /api/cases/{number}/transition
    // ============================================================
    [HttpPost("{number}/transition")]
    [Authorize(Policy = QmsPolicies.CasesWriteBasic)]
    public async Task<IActionResult> Transition([FromRoute] string number, [FromBody] CaseTransitionDto dto)
    {
        if (string.IsNullOrWhiteSpace(number))
            return BadRequest(new { Message = "Number is required." });

        var toCode = (dto?.ToStatusCode ?? "").Trim();
        if (string.IsNullOrWhiteSpace(toCode))
            return BadRequest(new { Message = "ToStatusCode je obavezan." });

        var tenantId = _tenant.TenantId;
        number = number.Trim();
        toCode = toCode.ToUpperInvariant();

        var header = await _db.vw_QmsIssueLists
            .AsNoTracking()
            .Where(x => x.TenantId == tenantId && x.Number == number)
            .Select(x => new { x.Number, x.EntityType, x.EntityId, x.WorkflowStatusId })
            .FirstOrDefaultAsync();

        if (header == null)
            return NotFound(new { Message = $"Case '{number}' nije pronađen." });

        var entityType = (header.EntityType ?? "").Trim().ToUpperInvariant();
        if (entityType != "COMPLAINT" && entityType != "NONCONFORMITY")
            return BadRequest(new { Message = "Nepoznat EntityType za slučaj." });

        var current = await _db.QmsWorkflowStatuses
            .AsNoTracking()
            .Where(x => x.TenantId == tenantId && x.IsActive == true && x.Id == header.WorkflowStatusId)
            .Select(x => new { x.Id, x.Code, x.IsInitial, x.IsFinal, x.IsCancelled })
            .FirstOrDefaultAsync();

        if (current == null)
            return BadRequest(new { Message = "Trenutni workflow status nije pronađen." });

        var target = await _db.QmsWorkflowStatuses
            .AsNoTracking()
            .Where(x =>
                x.TenantId == tenantId &&
                x.IsActive == true &&
                x.EntityType == entityType &&
                x.Code != null &&
                x.Code.ToUpper() == toCode)
            .Select(x => new { x.Id, x.Code, x.Name, x.IsInitial, x.IsFinal, x.IsCancelled })
            .FirstOrDefaultAsync();

        if (target == null)
            return BadRequest(new { Message = $"Ciljani status '{toCode}' ne postoji za '{entityType}'." });

        if (target.Id == current.Id)
            return NoContent();

        if (current.IsCancelled)
            return BadRequest(new { Message = "Slučaj je otkazan. Nije dopuštena promjena statusa." });

        if (current.IsFinal)
            return BadRequest(new { Message = "Slučaj je završen. Nije dopuštena promjena statusa." });

        var currentPhase = PhaseFromFlags(current.IsInitial, current.IsFinal, current.IsCancelled);
        var targetPhase = PhaseFromFlags(target.IsInitial, target.IsFinal, target.IsCancelled);

        var allowed =
            (currentPhase == "RECEIVED" && targetPhase == "IN_PROGRESS") ||
            (currentPhase == "IN_PROGRESS" && targetPhase == "CLOSED") ||
            ((currentPhase == "RECEIVED" || currentPhase == "IN_PROGRESS") && targetPhase == "CANCELLED");

        if (!allowed)
        {
            return BadRequest(new
            {
                Message = $"Nedopuštena tranzicija: {currentPhase} -> {targetPhase} (status {current.Code} -> {target.Code})."
            });
        }

        if (targetPhase == "CLOSED")
        {
            var hasOpenActions = await _db.vw_QmsIssue_Actions
                .AsNoTracking()
                .AnyAsync(a =>
                    a.TenantId == tenantId &&
                    a.IssueNumber == number &&
                    a.IsDeleted == false &&
                    a.CompletedDate == null);

            if (hasOpenActions)
            {
                return BadRequest(new
                {
                    Message = "Ne može se zatvoriti slučaj dok postoje otvorene radnje (CompletedDate nije postavljen)."
                });
            }
        }

        var issueQuery = _db.QmsIssues.Where(x => x.TenantId == tenantId && x.EntityType == entityType);

        if (entityType == "COMPLAINT")
            issueQuery = issueQuery.Where(x => x.ComplaintId == header.EntityId);
        else
            issueQuery = issueQuery.Where(x => x.NonconformityId == header.EntityId);

        var issue = await issueQuery.FirstOrDefaultAsync();

        if (issue == null)
            return BadRequest(new { Message = "QmsIssue zapis nije pronađen (ne mogu spremiti status)." });

        var changedById = User.TryGetUserId();
        var changedByName = User.TryGetDisplayName();
        var changedAtUtc = DateTime.UtcNow;

        _db.QmsIssueHistories.Add(new Models.Tables.QmsIssueHistory
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            IssueId = issue.Id,
            FromStatusCode = (current.Code ?? "").Trim(),
            ToStatusCode = (target.Code ?? "").Trim(),
            Phase = targetPhase,
            ChangedAt = changedAtUtc,
            ChangedByUserId = changedById,
            ChangedByName = changedByName
        });

        issue.WorkflowStatusId = target.Id;
        issue.UpdatedAt = changedAtUtc;
        issue.UpdatedByUserId = changedById;

        await _db.SaveChangesAsync();

        return Ok(new
        {
            Number = number,
            EntityType = entityType,
            FromStatusCode = current.Code,
            ToStatusCode = target.Code,
            ToStatusName = target.Name,
            Phase = targetPhase
        });
    }
}
