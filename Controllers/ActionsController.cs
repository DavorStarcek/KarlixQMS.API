using System.Text.Json;
using KarlixQMS.API.Data;
using KarlixQMS.API.Infrastructure;
using KarlixQMS.API.Infrastructure.Security;
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

    // ----------------------------
    // DTOs (stabilni contract)
    // ----------------------------

    public sealed class ActionListItemDto
    {
        public Guid ActionId { get; set; }
        public string? IssueNumber { get; set; }
        public string? EntityType { get; set; }

        public string? ActionTitle { get; set; }
        public string? ActionTypeName { get; set; }
        public string? ResponsibleName { get; set; }

        public DateTime? DueDate { get; set; }
        public DateTime? CompletedDate { get; set; }
        public DateTime? VerificationDate { get; set; }

        public string? StatusCode { get; set; }
        public int? DaysLate { get; set; }
    }

    public sealed class ActionDetailsDto
    {
        public Guid ActionId { get; set; }

        public string? EntityType { get; set; }
        public string? EntityNumber { get; set; }
        public string? EntityTitle { get; set; }

        public string? Title { get; set; }
        public string? Description { get; set; }

        public DateTime? DueDate { get; set; }
        public DateTime? CompletedDate { get; set; }

        public string? ResponsibleName { get; set; }

        public Guid? ResponsibleOrgUnitId { get; set; }
        public string? OrgUnitCode { get; set; }
        public string? OrgUnitName { get; set; }

        // ✅ za Edit UI trebaju ID-jevi
        public Guid ActionTypeId { get; set; }
        public Guid? EffectivenessId { get; set; }

        public string? ActionTypeCode { get; set; }
        public string? ActionTypeName { get; set; }

        public string? EffectivenessCode { get; set; }
        public string? EffectivenessName { get; set; }

        public DateTime? VerificationDate { get; set; }
        public string? VerificationNotes { get; set; }
    }

    // ============================================================
    // LISTA
    // ============================================================
    [HttpGet]
    [Authorize(Policy = QmsPolicies.ActionsRead)]
    public async Task<ActionResult<List<ActionListItemDto>>> Get(
        [FromQuery] bool openOnly = true,
        [FromQuery] bool? overdue = null,
        [FromQuery] bool awaitingVerification = false,
        [FromQuery] string? type = null,
        [FromQuery] string? caseNumber = null,
        [FromQuery] int take = 200)
    {
        var tenantId = _tenant.TenantId;
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        var q = _db.vw_QmsActionOverviews
            .AsNoTracking()
            .Where(x => x.TenantId == tenantId && x.IsActive == true);

        if (!string.IsNullOrWhiteSpace(type))
            q = q.Where(x => x.EntityType == type);

        if (!string.IsNullOrWhiteSpace(caseNumber))
        {
            var pattern = $"%{caseNumber.Trim()}%";
            q = q.Where(x => x.IssueNumber != null && EF.Functions.Like(x.IssueNumber, pattern));
        }

        if (awaitingVerification)
        {
            q = q.Where(x => x.CompletedDate != null && x.VerificationDate == null);
        }
        else
        {
            if (openOnly)
                q = q.Where(x => x.CompletedDate == null);

            if (overdue.HasValue)
            {
                if (overdue.Value)
                    q = q.Where(x => x.CompletedDate == null && x.DueDate != null && x.DueDate < today);
                else
                    q = q.Where(x => x.CompletedDate == null && (x.DueDate == null || x.DueDate >= today));
            }
        }

        var items = await q
            .OrderBy(x => x.DueDate == null)
            .ThenBy(x => x.DueDate)
            .Take(Math.Clamp(take, 1, 1000))
            .Select(x => new ActionListItemDto
            {
                ActionId = x.ActionId,
                IssueNumber = x.IssueNumber,
                EntityType = x.EntityType,

                ActionTitle = x.ActionTitle,
                ActionTypeName = x.ActionTypeName,
                ResponsibleName = x.ResponsibleName,

                DueDate = x.DueDate.HasValue ? x.DueDate.Value.ToDateTime(TimeOnly.MinValue) : (DateTime?)null,
                CompletedDate = x.CompletedDate.HasValue ? x.CompletedDate.Value.ToDateTime(TimeOnly.MinValue) : (DateTime?)null,
                VerificationDate = x.VerificationDate.HasValue ? x.VerificationDate.Value.ToDateTime(TimeOnly.MinValue) : (DateTime?)null,

                StatusCode =
                    awaitingVerification ? "AWAITING_VERIFICATION" :
                    (x.CompletedDate != null) ? "DONE" :
                    (x.CompletedDate == null && x.DueDate != null && x.DueDate < today) ? "OVERDUE" :
                    "IN_PROGRESS",

                DaysLate =
                    (x.CompletedDate == null && x.DueDate != null && x.DueDate < today)
                        ? (today.DayNumber - x.DueDate.Value.DayNumber)
                        : (int?)null
            })
            .ToListAsync();

        return Ok(items);
    }

    // ============================================================
    // DETAILS
    // (JOIN tablice + view) -> daje ID-jeve za Edit UI
    // ============================================================
    [HttpGet("{id:guid}")]
    [Authorize(Policy = QmsPolicies.ActionsRead)]
    public async Task<ActionResult<ActionDetailsDto>> GetById([FromRoute] Guid id)
    {
        var tenantId = _tenant.TenantId;

        var dto = await (
            from a in _db.QmsIssueActions.AsNoTracking()
            join v in _db.vw_QmsActionOverviews.AsNoTracking()
                on a.Id equals v.ActionId
            where a.TenantId == tenantId
                  && a.Id == id
                  && a.IsActive == true
                  && a.IsDeleted == false
                  && v.TenantId == tenantId
                  && v.IsActive == true
            select new ActionDetailsDto
            {
                ActionId = a.Id,

                EntityType = v.EntityType,
                EntityNumber = v.IssueNumber,
                EntityTitle = v.IssueTitle,

                Title = a.Title,
                Description = a.Description,

                DueDate = a.DueDate.HasValue ? a.DueDate.Value.ToDateTime(TimeOnly.MinValue) : (DateTime?)null,
                CompletedDate = a.CompletedDate.HasValue ? a.CompletedDate.Value.ToDateTime(TimeOnly.MinValue) : (DateTime?)null,

                ResponsibleName = a.ResponsibleName,

                ResponsibleOrgUnitId = a.ResponsibleOrgUnitId,
                OrgUnitCode = v.ResponsibleOrgUnitCode,
                OrgUnitName = v.ResponsibleOrgUnitName,

                ActionTypeId = a.ActionTypeId,
                EffectivenessId = a.EffectivenessId,

                ActionTypeCode = v.ActionTypeCode,
                ActionTypeName = v.ActionTypeName,

                EffectivenessCode = v.EffectivenessCode,
                EffectivenessName = v.EffectivenessName,

                VerificationDate = a.VerificationDate.HasValue ? a.VerificationDate.Value.ToDateTime(TimeOnly.MinValue) : (DateTime?)null,
                VerificationNotes = a.VerificationNotes
            })
            .FirstOrDefaultAsync();

        if (dto == null)
            return NotFound(new { Message = $"Action '{id}' nije pronađena." });

        return Ok(dto);
    }

    // ============================================================
    // UPDATE (PATCH-like preko PUT, s razlikovanjem missing vs null)
    // ============================================================
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update([FromRoute] Guid id, [FromBody] JsonElement payload)
    {
        var tenantId = _tenant.TenantId;

        var action = await _db.QmsIssueActions
            .FirstOrDefaultAsync(x =>
                x.TenantId == tenantId &&
                x.Id == id &&
                x.IsActive == true &&
                x.IsDeleted == false);

        if (action == null)
            return NotFound(new { Message = $"Action '{id}' nije pronađena." });

        bool isAdminRole = User.IsInRole("GlobalAdmin") || User.IsInRole("TenantAdmin");

        // helpers -------------------------------------------------
        static bool Has(JsonElement root, string name, out JsonElement value)
        {
            if (root.ValueKind == JsonValueKind.Object && root.TryGetProperty(name, out value))
                return true;
            value = default;
            return false;
        }

        static string? GetStringOrNull(JsonElement el)
        {
            return el.ValueKind switch
            {
                JsonValueKind.Null => null,
                JsonValueKind.String => el.GetString(),
                _ => el.ToString()
            };
        }

        static Guid? GetGuidOrNull(JsonElement el)
        {
            if (el.ValueKind == JsonValueKind.Null) return null;
            if (el.ValueKind == JsonValueKind.String && Guid.TryParse(el.GetString(), out var g)) return g;
            return null;
        }

        static DateTime? GetDateTimeOrNull(JsonElement el)
        {
            if (el.ValueKind == JsonValueKind.Null) return null;
            if (el.ValueKind == JsonValueKind.String && DateTime.TryParse(el.GetString(), out var d)) return d;
            return null;
        }

        static DateOnly? ToDateOnly(DateTime? dt) => dt.HasValue ? DateOnly.FromDateTime(dt.Value) : (DateOnly?)null;

        // snapshot prije promjene ----------------------------------
        var beforeTitle = action.Title;
        var beforeDesc = action.Description;
        var beforeDue = action.DueDate;
        var beforeCompleted = action.CompletedDate;
        var beforeResp = action.ResponsibleName;
        var beforeOrg = action.ResponsibleOrgUnitId;
        var beforeTypeId = action.ActionTypeId;
        var beforeEff = action.EffectivenessId;
        var beforeVerDate = action.VerificationDate;
        var beforeVerNotes = action.VerificationNotes;

        // primjena promjena (missing = ignore, null = set null) ----
        if (Has(payload, "title", out var pTitle))
        {
            var t = GetStringOrNull(pTitle);
            // title je obavezan, ako je poslan kao null/"" -> 400
            if (string.IsNullOrWhiteSpace(t))
                return BadRequest(new { Message = "Title je obavezan." });

            action.Title = t.Trim();
        }

        if (Has(payload, "description", out var pDesc))
        {
            // Description je NOT NULL u bazi -> ako pošalje null, spremi "" (ili promijeni ako želiš strict 400)
            var d = GetStringOrNull(pDesc);
            action.Description = d ?? "";
        }

        if (Has(payload, "dueDate", out var pDue))
        {
            var dt = GetDateTimeOrNull(pDue);
            action.DueDate = ToDateOnly(dt); // null -> NULL u bazi
        }

        if (Has(payload, "completedDate", out var pCompleted))
        {
            var dt = GetDateTimeOrNull(pCompleted);
            action.CompletedDate = ToDateOnly(dt); // null -> NULL u bazi
        }

        if (Has(payload, "responsibleName", out var pResp))
        {
            action.ResponsibleName = GetStringOrNull(pResp); // null -> NULL
        }

        if (Has(payload, "responsibleOrgUnitId", out var pOrg))
        {
            action.ResponsibleOrgUnitId = GetGuidOrNull(pOrg); // null -> NULL
        }

        if (Has(payload, "actionTypeId", out var pTypeId))
        {
            var g = GetGuidOrNull(pTypeId);
            // ActionTypeId je NOT NULL -> ako pošalje null -> 400
            if (!g.HasValue)
                return BadRequest(new { Message = "ActionTypeId je obavezan." });

            action.ActionTypeId = g.Value;
        }

        // effectiveness/verifikacija --------------------------------
        if (Has(payload, "effectivenessId", out var pEff))
        {
            action.EffectivenessId = GetGuidOrNull(pEff); // null -> NULL
        }

        if (Has(payload, "verificationDate", out var pVerDate))
        {
            var dt = GetDateTimeOrNull(pVerDate);
            action.VerificationDate = ToDateOnly(dt); // null -> NULL
        }

        if (Has(payload, "verificationNotes", out var pVerNotes))
        {
            action.VerificationNotes = GetStringOrNull(pVerNotes); // null -> NULL
        }

        // workflow pravilo: verifikacija tek nakon completed --------
        // (vrijedi i ako completed već postoji u bazi)
        if (action.VerificationDate.HasValue && !action.CompletedDate.HasValue)
            return BadRequest(new { Message = "Verifikacija je moguća tek kad je radnja završena (CompletedDate)." });

        // ako je radnja “un-complete”, očisti verification/effectiveness
        if (!action.CompletedDate.HasValue)
        {
            action.EffectivenessId = null;
            action.VerificationDate = null;
            action.VerificationNotes = null;
        }

        // autorizacija po promjeni ----------------------------------
        bool basicChanged =
            !string.Equals(beforeTitle, action.Title, StringComparison.Ordinal) ||
            !string.Equals(beforeDesc, action.Description, StringComparison.Ordinal) ||
            beforeDue != action.DueDate ||
            beforeCompleted != action.CompletedDate ||
            !string.Equals(beforeResp, action.ResponsibleName, StringComparison.Ordinal) ||
            beforeOrg != action.ResponsibleOrgUnitId ||
            beforeTypeId != action.ActionTypeId;

        bool verifyChanged =
            beforeEff != action.EffectivenessId ||
            beforeVerDate != action.VerificationDate ||
            !string.Equals(beforeVerNotes, action.VerificationNotes, StringComparison.Ordinal);

        if (basicChanged && !isAdminRole && !User.HasClaim("perm", QmsPerms.ActionsWriteBasic))
            return Forbid();

        if (verifyChanged && !isAdminRole && !User.HasClaim("perm", QmsPerms.ActionsVerify))
            return Forbid();

        action.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return NoContent();
    }
}
