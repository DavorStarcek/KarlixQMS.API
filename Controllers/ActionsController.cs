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
        public string? Title { get; set; }
        public string? Description { get; set; }

        public string? EntityType { get; set; }
        public string? EntityNumber { get; set; }
        public string? EntityTitle { get; set; }

        public DateTime? DueDate { get; set; }
        public DateTime? CompletedDate { get; set; }

        public string? ResponsibleName { get; set; }

        public Guid? ResponsibleOrgUnitId { get; set; }
        public string? OrgUnitCode { get; set; }
        public string? OrgUnitName { get; set; }

        public string? ActionTypeCode { get; set; }
        public string? ActionTypeName { get; set; }

        public string? EffectivenessCode { get; set; }
        public string? EffectivenessName { get; set; }

        public DateTime? VerificationDate { get; set; }
        public string? VerificationNotes { get; set; }
    }

    public sealed class ActionUpdateDto
    {
        public string? Title { get; set; }
        public string? Description { get; set; }

        public DateTime? DueDate { get; set; }
        public DateTime? CompletedDate { get; set; }

        public string? ResponsibleName { get; set; }
        public Guid? ResponsibleOrgUnitId { get; set; }

        public Guid? ActionTypeId { get; set; } // UI šalje Guid? (select može biti prazno)
        public Guid? EffectivenessId { get; set; }

        public DateTime? VerificationDate { get; set; }
        public string? VerificationNotes { get; set; }
    }

    // ----------------------------
    // LISTA
    // GET /api/actions?openOnly=true&overdue=true&type=COMPLAINT&caseNumber=RIN-1234&awaitingVerification=true
    // ----------------------------
    [HttpGet]
    public async Task<ActionResult<List<ActionListItemDto>>> Get(
        [FromQuery] bool openOnly = true,
        [FromQuery] bool? overdue = null,
        [FromQuery] bool awaitingVerification = false,
        [FromQuery] string? type = null,       // COMPLAINT / NONCONFORMITY
        [FromQuery] string? caseNumber = null, // IssueNumber (RIN-xxxx / UN-xxxx)
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

        // ✅ ČEKA VERIFIKACIJU: završeno, ali bez verifikacije
        // (ovo su završene radnje, pa openOnly/overdue nema smisla i namjerno ih ignoriramo)
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
            .OrderBy(x => x.DueDate == null)  // non-null prvo
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

    // ----------------------------
    // DETAILS
    // GET /api/actions/{id}
    // ----------------------------
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ActionDetailsDto>> GetById([FromRoute] Guid id)
    {
        var tenantId = _tenant.TenantId;

        var row = await _db.vw_QmsActionOverviews
            .AsNoTracking()
            .Where(x => x.TenantId == tenantId && x.ActionId == id && x.IsActive == true)
            .Select(x => new ActionDetailsDto
            {
                ActionId = x.ActionId,
                Title = x.ActionTitle,
                Description = x.ActionDescription,

                EntityType = x.EntityType,
                EntityNumber = x.IssueNumber,
                EntityTitle = x.IssueTitle,

                DueDate = x.DueDate.HasValue ? x.DueDate.Value.ToDateTime(TimeOnly.MinValue) : (DateTime?)null,
                CompletedDate = x.CompletedDate.HasValue ? x.CompletedDate.Value.ToDateTime(TimeOnly.MinValue) : (DateTime?)null,

                ResponsibleName = x.ResponsibleName,

                ResponsibleOrgUnitId = x.ResponsibleOrgUnitId,
                OrgUnitCode = x.ResponsibleOrgUnitCode,
                OrgUnitName = x.ResponsibleOrgUnitName,

                ActionTypeCode = x.ActionTypeCode,
                ActionTypeName = x.ActionTypeName,

                EffectivenessCode = x.EffectivenessCode,
                EffectivenessName = x.EffectivenessName,

                VerificationDate = x.VerificationDate.HasValue ? x.VerificationDate.Value.ToDateTime(TimeOnly.MinValue) : (DateTime?)null,
                VerificationNotes = x.VerificationNotes
            })
            .FirstOrDefaultAsync();

        if (row == null)
            return NotFound(new { Message = $"Action '{id}' nije pronađena." });

        return Ok(row);
    }

    // ----------------------------
    // UPDATE
    // PUT /api/actions/{id}
    // ----------------------------
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update([FromRoute] Guid id, [FromBody] ActionUpdateDto dto)
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

        // ---- VALIDACIJE ----
        var title = (dto.Title ?? "").Trim();
        if (string.IsNullOrWhiteSpace(title))
            return BadRequest(new { Message = "Title je obavezan." });

        if (!dto.ActionTypeId.HasValue)
            return BadRequest(new { Message = "ActionTypeId je obavezan." });

        // workflow: verifikacija tek nakon completed
        if (dto.VerificationDate.HasValue && !dto.CompletedDate.HasValue)
            return BadRequest(new { Message = "Verifikacija je moguća tek kad je radnja završena (CompletedDate)." });

        var completedDateOnly = dto.CompletedDate.HasValue
            ? DateOnly.FromDateTime(dto.CompletedDate.Value)
            : (DateOnly?)null;

        // ---- MAPIRANJE ----
        action.Title = title;

        // Description u bazi je NOT NULL:
        action.Description = dto.Description ?? "";

        action.DueDate = dto.DueDate.HasValue
            ? DateOnly.FromDateTime(dto.DueDate.Value)
            : (DateOnly?)null;

        action.CompletedDate = completedDateOnly;

        action.ResponsibleName = dto.ResponsibleName;
        action.ResponsibleOrgUnitId = dto.ResponsibleOrgUnitId;

        // ActionTypeId u bazi je NOT NULL:
        action.ActionTypeId = dto.ActionTypeId.Value;

        if (completedDateOnly == null)
        {
            // ako se “un-complete”, očisti verifikaciju/učinkovitost
            action.EffectivenessId = null;
            action.VerificationDate = null;
            action.VerificationNotes = null;
        }
        else
        {
            action.EffectivenessId = dto.EffectivenessId;

            action.VerificationDate = dto.VerificationDate.HasValue
                ? DateOnly.FromDateTime(dto.VerificationDate.Value)
                : (DateOnly?)null;

            action.VerificationNotes = dto.VerificationNotes;
        }

        action.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return NoContent(); // 204
    }
}
