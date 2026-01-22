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
    // DTOs
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

        // ✅ za badge
        public string? EffectivenessCode { get; set; }
        public bool? IsDeleted { get; set; }

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

        public Guid? ActionTypeId { get; set; }
        public string? ActionTypeCode { get; set; }
        public string? ActionTypeName { get; set; }

        public Guid? EffectivenessId { get; set; }
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

        public Guid? ActionTypeId { get; set; }
        public Guid? EffectivenessId { get; set; }

        public DateTime? VerificationDate { get; set; }
        public string? VerificationNotes { get; set; }
    }

    public sealed class ActionVerificationPatchDto
    {
        public DateTime? VerificationDate { get; set; }
        public string? VerificationNotes { get; set; }
        public Guid? EffectivenessId { get; set; }
    }

    // ----------------------------
    // LISTA
    // GET /api/actions?openOnly=true&overdue=true&type=COMPLAINT&caseNumber=RIN-1234&awaitingVerification=true
    // ----------------------------
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

        // ✅ join na tablicu da dobijemo IsDeleted (i da budemo konzistentni s Details)
        var q =
            from v in _db.vw_QmsActionOverviews.AsNoTracking()
            join a in _db.QmsIssueActions.AsNoTracking()
                on v.ActionId equals a.Id
            where v.TenantId == tenantId
               && v.IsActive == true
               && a.TenantId == tenantId
               && a.IsActive == true
            select new { v, a };

        // default: sakrij obrisane
        q = q.Where(x => x.a.IsDeleted == false);

        if (!string.IsNullOrWhiteSpace(type))
            q = q.Where(x => x.v.EntityType == type);

        if (!string.IsNullOrWhiteSpace(caseNumber))
        {
            var pattern = $"%{caseNumber.Trim()}%";
            q = q.Where(x => x.v.IssueNumber != null && EF.Functions.Like(x.v.IssueNumber, pattern));
        }

        // ✅ čeka verifikaciju
        if (awaitingVerification)
        {
            q = q.Where(x => x.v.CompletedDate != null && x.v.VerificationDate == null);
        }
        else
        {
            if (openOnly)
                q = q.Where(x => x.v.CompletedDate == null);

            if (overdue.HasValue)
            {
                if (overdue.Value)
                    q = q.Where(x => x.v.CompletedDate == null && x.v.DueDate != null && x.v.DueDate < today);
                else
                    q = q.Where(x => x.v.CompletedDate == null && (x.v.DueDate == null || x.v.DueDate >= today));
            }
        }

        var items = await q
            .OrderBy(x => x.v.DueDate == null)
            .ThenBy(x => x.v.DueDate)
            .Take(Math.Clamp(take, 1, 1000))
            .Select(x => new ActionListItemDto
            {
                ActionId = x.v.ActionId,
                IssueNumber = x.v.IssueNumber,
                EntityType = x.v.EntityType,

                ActionTitle = x.v.ActionTitle,
                ActionTypeName = x.v.ActionTypeName,
                ResponsibleName = x.v.ResponsibleName,

                DueDate = x.v.DueDate.HasValue ? x.v.DueDate.Value.ToDateTime(TimeOnly.MinValue) : (DateTime?)null,
                CompletedDate = x.v.CompletedDate.HasValue ? x.v.CompletedDate.Value.ToDateTime(TimeOnly.MinValue) : (DateTime?)null,
                VerificationDate = x.v.VerificationDate.HasValue ? x.v.VerificationDate.Value.ToDateTime(TimeOnly.MinValue) : (DateTime?)null,

                // ✅ badge
                EffectivenessCode = x.v.EffectivenessCode,
                IsDeleted = x.a.IsDeleted,

                StatusCode =
                    awaitingVerification ? "AWAITING_VERIFICATION" :
                    (x.v.CompletedDate != null) ? "DONE" :
                    (x.v.CompletedDate == null && x.v.DueDate != null && x.v.DueDate < today) ? "OVERDUE" :
                    "IN_PROGRESS",

                DaysLate =
                    (x.v.CompletedDate == null && x.v.DueDate != null && x.v.DueDate < today)
                        ? (today.DayNumber - x.v.DueDate.Value.DayNumber)
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
    [Authorize(Policy = QmsPolicies.ActionsRead)]
    public async Task<ActionResult<ActionDetailsDto>> GetById([FromRoute] Guid id)
    {
        var tenantId = _tenant.TenantId;

        var row = await (
            from v in _db.vw_QmsActionOverviews.AsNoTracking()
            join a in _db.QmsIssueActions.AsNoTracking()
                on v.ActionId equals a.Id
            where v.TenantId == tenantId
               && v.ActionId == id
               && v.IsActive == true
               && a.TenantId == tenantId
               && a.IsActive == true
               && a.IsDeleted == false
            select new ActionDetailsDto
            {
                ActionId = v.ActionId,

                Title = v.ActionTitle,
                Description = v.ActionDescription,

                EntityType = v.EntityType,
                EntityNumber = v.IssueNumber,
                EntityTitle = v.IssueTitle,

                DueDate = v.DueDate.HasValue ? v.DueDate.Value.ToDateTime(TimeOnly.MinValue) : (DateTime?)null,
                CompletedDate = v.CompletedDate.HasValue ? v.CompletedDate.Value.ToDateTime(TimeOnly.MinValue) : (DateTime?)null,

                ResponsibleName = v.ResponsibleName,
                ResponsibleOrgUnitId = v.ResponsibleOrgUnitId,
                OrgUnitCode = v.ResponsibleOrgUnitCode,
                OrgUnitName = v.ResponsibleOrgUnitName,

                ActionTypeId = a.ActionTypeId,
                ActionTypeCode = v.ActionTypeCode,
                ActionTypeName = v.ActionTypeName,

                EffectivenessId = a.EffectivenessId,
                EffectivenessCode = v.EffectivenessCode,
                EffectivenessName = v.EffectivenessName,

                VerificationDate = v.VerificationDate.HasValue ? v.VerificationDate.Value.ToDateTime(TimeOnly.MinValue) : (DateTime?)null,
                VerificationNotes = v.VerificationNotes
            }
        ).FirstOrDefaultAsync();

        if (row == null)
            return NotFound(new { Message = $"Action '{id}' nije pronađena." });

        return Ok(row);
    }

    // ----------------------------
    // UPDATE (FULL)
    // PUT /api/actions/{id}
    // ----------------------------
    [HttpPut("{id:guid}")]
    [Authorize(Policy = QmsPolicies.ActionsWriteBasic)]
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

        var title = (dto.Title ?? "").Trim();
        if (string.IsNullOrWhiteSpace(title))
            return BadRequest(new { Message = "Title je obavezan." });

        if (!dto.ActionTypeId.HasValue)
            return BadRequest(new { Message = "ActionTypeId je obavezan." });

        if (dto.VerificationDate.HasValue && !dto.CompletedDate.HasValue)
            return BadRequest(new { Message = "Verifikacija je moguća tek kad je radnja završena (CompletedDate)." });

        var completedDateOnly = dto.CompletedDate.HasValue
            ? DateOnly.FromDateTime(dto.CompletedDate.Value)
            : (DateOnly?)null;

        action.Title = title;
        action.Description = dto.Description ?? "";

        action.DueDate = dto.DueDate.HasValue
            ? DateOnly.FromDateTime(dto.DueDate.Value)
            : (DateOnly?)null;

        action.CompletedDate = completedDateOnly;

        action.ResponsibleName = dto.ResponsibleName;
        action.ResponsibleOrgUnitId = dto.ResponsibleOrgUnitId;

        action.ActionTypeId = dto.ActionTypeId.Value;

        if (completedDateOnly == null)
        {
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
        return NoContent();
    }

    // ----------------------------
    // PATCH-like: VERIFICATION ONLY
    // PATCH /api/actions/{id}/verification
    // ----------------------------
    [HttpPatch("{id:guid}/verification")]
    [Authorize(Policy = QmsPolicies.ActionsVerify)]
    public async Task<IActionResult> PatchVerification([FromRoute] Guid id, [FromBody] ActionVerificationPatchDto dto)
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

        if (action.CompletedDate == null)
            return BadRequest(new { Message = "Verifikacija je moguća tek kad je radnja završena (CompletedDate)." });

        action.VerificationNotes = dto.VerificationNotes;

        action.VerificationDate = dto.VerificationDate.HasValue
            ? DateOnly.FromDateTime(dto.VerificationDate.Value)
            : (DateOnly?)null;

        action.EffectivenessId = dto.EffectivenessId;

        action.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return NoContent();
    }
}
