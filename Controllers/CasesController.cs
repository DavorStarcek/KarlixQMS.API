using System.Text.Json;
using KarlixQMS.API.Data;
using KarlixQMS.API.Infrastructure;
using KarlixQMS.API.Infrastructure.Security;
using KarlixQMS.API.Models.Tables;
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
                a.EffectivenessName
            })
            .ToListAsync();

        return Ok(new
        {
            header.Number,
            header.EntityType,
            header.Title,
            header.StatusCode,
            header.StatusName,
            header.IssueDate,
            header.CustomerName,
            Actions = actions
        });
    }

    // ============================================================
    // PATCH-like: FIELDS (partial update)
    // PATCH /api/cases/{number}/fields
    //
    // Body: JSON s bilo kojim subsetom polja.
    // - property postoji s null -> upiši NULL
    // - property ne postoji -> ne diraj
    //
    // Authorization:
    // - Admin (GlobalAdmin/TenantAdmin) uvijek može
    // - Ostali moraju imati qms.rin.write.{STATUSCODE} ili qms.un.write.{STATUSCODE}
    // ============================================================
    [HttpPatch("{number}/fields")]
    public async Task<IActionResult> PatchFields([FromRoute] string number, [FromBody] JsonElement body)
    {
        if (string.IsNullOrWhiteSpace(number))
            return BadRequest(new { Message = "Number is required." });

        if (body.ValueKind != JsonValueKind.Object)
            return BadRequest(new { Message = "Body must be a JSON object." });

        var tenantId = _tenant.TenantId;
        number = number.Trim();

        // Nađi case kroz vw (da dobijemo EntityType + WorkflowStatusId)
        var header = await _db.vw_QmsIssueLists
            .AsNoTracking()
            .Where(x => x.TenantId == tenantId && x.Number == number)
            .Select(x => new
            {
                x.EntityId,
                x.Number,
                x.EntityType,
                x.WorkflowStatusId
            })
            .FirstOrDefaultAsync();

        if (header == null)
            return NotFound(new { Message = $"Case '{number}' nije pronađen." });

        var entityType = (header.EntityType ?? "").Trim().ToUpperInvariant();
        if (entityType != "COMPLAINT" && entityType != "NONCONFORMITY")
            return BadRequest(new { Message = $"Unsupported EntityType '{header.EntityType}'." });

        // current workflow status code
        var statusCode = await _db.QmsWorkflowStatuses
            .AsNoTracking()
            .Where(ws => ws.Id == header.WorkflowStatusId && ws.TenantId == tenantId && ws.IsActive == true)
            .Select(ws => ws.Code)
            .FirstOrDefaultAsync();

        statusCode = (statusCode ?? "").Trim().ToUpperInvariant();
        if (string.IsNullOrWhiteSpace(statusCode))
            return BadRequest(new { Message = "Workflow status code not found for this case." });

        // Permission check (admin bypass; others need phase perm)
        if (!IsAdmin(User))
        {
            var requiredPerm = entityType == "COMPLAINT"
                ? $"qms.rin.write.{statusCode}"
                : $"qms.un.write.{statusCode}";

            if (!User.HasClaim("perm", requiredPerm) && !User.HasClaim("perm", QmsPerms.QmsAdmin))
                return Forbid();
        }

        // Load entity + patch map
        if (entityType == "COMPLAINT")
        {
            // Complaint entity id je header.EntityId
            var e = await _db.QmsCustomerComplaints
                .FirstOrDefaultAsync(x => x.TenantId == tenantId && x.Id == header.EntityId);

            if (e == null)
                return NotFound(new { Message = $"Complaint '{number}' nije pronađena." });

            ApplyComplaintPatch(e, body);
            e.LastModifiedAt = DateTime.UtcNow;
            e.LastModifiedBy = User.Identity?.Name;
        }
        else
        {
            var e = await _db.QmsNonconformities
                .FirstOrDefaultAsync(x => x.TenantId == tenantId && x.Id == header.EntityId);

            if (e == null)
                return NotFound(new { Message = $"Nonconformity '{number}' nije pronađena." });

            ApplyNonconformityPatch(e, body);
            e.LastModifiedAt = DateTime.UtcNow;
            e.LastModifiedBy = User.Identity?.Name;
        }

        await _db.SaveChangesAsync();
        return NoContent();
    }

    // ============================================================
    // PATCH HELPERS
    // ============================================================

    private static bool IsAdmin(System.Security.Claims.ClaimsPrincipal user) =>
        user.IsInRole("GlobalAdmin") || user.IsInRole("TenantAdmin");

    // ---- Complaint patch: minimalno bitna polja za faze (RECEIVED/IN_PROGRESS/CLOSED)
    // Ako ti kasnije želiš “strože po fazama”, to ćemo dodati (mapa allowed fields po statusCode).
    private static void ApplyComplaintPatch(QmsCustomerComplaint e, JsonElement body)
    {
        // strings (null allowed)
        SetString(body, "title", v => e.Title = v);
        SetString(body, "description", v => e.Description = v);
        SetString(body, "analysis", v => e.Analysis = v);
        SetString(body, "rootCause", v => e.RootCause = v);
        SetString(body, "rootCauseInvestigatedBy", v => e.RootCauseInvestigatedBy = v);
        SetString(body, "justificationDetail", v => e.JustificationDetail = v);
        SetString(body, "immediateCorrection", v => e.ImmediateCorrection = v);
        SetString(body, "feedbackToCustomer", v => e.FeedbackToCustomer = v);
        SetString(body, "feedbackToSales", v => e.FeedbackToSales = v);

        SetString(body, "customerCode", v => e.CustomerCode = v);
        SetString(body, "customerName", v => e.CustomerName = v);
        SetString(body, "customerAddress", v => e.CustomerAddress = v);
        SetString(body, "customerPhone", v => e.CustomerPhone = v);
        SetString(body, "customerEmail", v => e.CustomerEmail = v);

        SetString(body, "salesPointCode", v => e.SalesPointCode = v);
        SetString(body, "salesPointName", v => e.SalesPointName = v);

        SetString(body, "productCode", v => e.ProductCode = v);
        SetString(body, "productName", v => e.ProductName = v);
        SetString(body, "productLot", v => e.ProductLot = v);

        // Guid?
        SetGuid(body, "orgUnitId", v => e.OrgUnitId = v);
        SetGuid(body, "complaintReasonId", v => e.ComplaintReasonId = v);
        SetGuid(body, "complaintFindingTypeId", v => e.ComplaintFindingTypeId = v);
        SetGuid(body, "productStateId", v => e.ProductStateId = v);
        SetGuid(body, "unitOfMeasureId", v => e.UnitOfMeasureId = v);

        // bool / bool?
        SetBool(body, "sampleAvailable", v => e.SampleAvailable = v ?? false); // u modelu je bool, pa null tretiramo kao false
        SetString(body, "sampleDescription", v => e.SampleDescription = v);

        SetBoolNullable(body, "isComplaintJustified", v => e.IsComplaintJustified = v);

        // int? / decimal?
        SetInt(body, "healthRiskLevel", v => e.HealthRiskLevel = v);
        SetDecimal(body, "productQuantity", v => e.ProductQuantity = v);

        // DateOnly / DateOnly?
        SetDateOnly(body, "orderDate", v => e.OrderDate = v);
        SetDateOnly(body, "deliveryDate", v => e.DeliveryDate = v);
        SetDateOnly(body, "productExpiryDate", v => e.ProductExpiryDate = v);

        SetDateOnly(body, "closedDate", v => e.ClosedDate = v);

        // NOTE: ComplaintDate i ReceivedDate su non-nullable u modelu (DateOnly),
        // pa ih ovdje namjerno NE patchamo (da se ne dogodi “NULL” ili loš update).
        // Ako želiš da budu promjenjivi — reci i dodamo strogu validaciju.
    }

    private static void ApplyNonconformityPatch(QmsNonconformity e, JsonElement body)
    {
        // strings
        SetString(body, "title", v => e.Title = v ?? "");          // Title je NOT NULL u modelu
        SetString(body, "description", v => e.Description = v);
        SetString(body, "source", v => e.Source = v ?? e.Source);  // Source je NOT NULL, null ignoriramo
        SetString(body, "raisedBy", v => e.RaisedBy = v);

        SetString(body, "rootCause", v => e.RootCause = v);
        SetString(body, "rootCauseInvestigatedBy", v => e.RootCauseInvestigatedBy = v);

        SetString(body, "effectivenessComment", v => e.EffectivenessComment = v);
        SetString(body, "verifiedBy", v => e.VerifiedBy = v);

        SetString(body, "cause", v => e.Cause = v);
        SetString(body, "immediateCorrection", v => e.ImmediateCorrection = v);
        SetString(body, "correctiveAction", v => e.CorrectiveAction = v);
        SetString(body, "preventiveAction", v => e.PreventiveAction = v);

        // Guid?
        SetGuid(body, "orgUnitId", v => e.OrgUnitId = v);
        SetGuid(body, "relationTypeId", v => e.RelationTypeId = v);
        SetGuid(body, "auditId", v => e.AuditId = v);
        SetGuid(body, "standardRequirementId", v => e.StandardRequirementId = v);
        SetGuid(body, "effectivenessId", v => e.EffectivenessId = v);
        SetGuid(body, "productStateId", v => e.ProductStateId = v);
        SetGuid(body, "productId", v => e.ProductId = v);

        // DateOnly / DateOnly?
        SetDateOnly(body, "detectionDate", v => e.DetectionDate = v);
        SetDateOnly(body, "requestDate", v => e.RequestDate = v);
        SetDateOnly(body, "dueDate", v => e.DueDate = v);
        SetDateOnly(body, "closeDate", v => e.CloseDate = v);

        SetDateOnly(body, "verificationDate", v => e.VerificationDate = v);

        // RaisedAt je NOT NULL (DateOnly) -> ne patchamo bez dogovora.
    }

    // ============================================================
    // JSON PATCH UTIL (presence-aware)
    // ============================================================

    private static void SetString(JsonElement obj, string prop, Action<string?> set)
    {
        if (!obj.TryGetProperty(prop, out var p)) return;

        if (p.ValueKind == JsonValueKind.Null) { set(null); return; }
        if (p.ValueKind == JsonValueKind.String) { set(p.GetString()); return; }

        // fallback: ako dođe broj/bool, pretvori u string
        set(p.ToString());
    }

    private static void SetGuid(JsonElement obj, string prop, Action<Guid?> set)
    {
        if (!obj.TryGetProperty(prop, out var p)) return;

        if (p.ValueKind == JsonValueKind.Null) { set(null); return; }

        if (p.ValueKind == JsonValueKind.String)
        {
            var s = p.GetString();
            if (string.IsNullOrWhiteSpace(s)) { set(null); return; }
            if (Guid.TryParse(s, out var g)) { set(g); return; }
            return;
        }

        // ako je došao Guid kao object/number -> ignor
    }

    private static void SetBool(JsonElement obj, string prop, Action<bool?> set)
    {
        if (!obj.TryGetProperty(prop, out var p)) return;

        if (p.ValueKind == JsonValueKind.Null) { set(null); return; }
        if (p.ValueKind == JsonValueKind.True) { set(true); return; }
        if (p.ValueKind == JsonValueKind.False) { set(false); return; }

        if (p.ValueKind == JsonValueKind.String)
        {
            var s = p.GetString();
            if (bool.TryParse(s, out var b)) { set(b); return; }
        }
    }

    private static void SetBoolNullable(JsonElement obj, string prop, Action<bool?> set) => SetBool(obj, prop, set);

    private static void SetInt(JsonElement obj, string prop, Action<int?> set)
    {
        if (!obj.TryGetProperty(prop, out var p)) return;

        if (p.ValueKind == JsonValueKind.Null) { set(null); return; }

        if (p.ValueKind == JsonValueKind.Number)
        {
            if (p.TryGetInt32(out var i)) { set(i); return; }
        }

        if (p.ValueKind == JsonValueKind.String)
        {
            var s = p.GetString();
            if (int.TryParse(s, out var i)) { set(i); return; }
            if (string.IsNullOrWhiteSpace(s)) { set(null); return; }
        }
    }

    private static void SetDecimal(JsonElement obj, string prop, Action<decimal?> set)
    {
        if (!obj.TryGetProperty(prop, out var p)) return;

        if (p.ValueKind == JsonValueKind.Null) { set(null); return; }

        if (p.ValueKind == JsonValueKind.Number)
        {
            if (p.TryGetDecimal(out var d)) { set(d); return; }
        }

        if (p.ValueKind == JsonValueKind.String)
        {
            var s = p.GetString();
            if (decimal.TryParse(s, out var d)) { set(d); return; }
            if (string.IsNullOrWhiteSpace(s)) { set(null); return; }
        }
    }

    private static void SetDateOnly(JsonElement obj, string prop, Action<DateOnly?> set)
    {
        if (!obj.TryGetProperty(prop, out var p)) return;

        if (p.ValueKind == JsonValueKind.Null) { set(null); return; }

        if (p.ValueKind == JsonValueKind.String)
        {
            var s = p.GetString();
            if (string.IsNullOrWhiteSpace(s)) { set(null); return; }

            // očekujemo "yyyy-MM-dd" (što HTML date šalje)
            if (DateOnly.TryParse(s, out var d)) { set(d); return; }

            // fallback DateTime
            if (DateTime.TryParse(s, out var dt)) { set(DateOnly.FromDateTime(dt)); return; }
        }
    }
}
