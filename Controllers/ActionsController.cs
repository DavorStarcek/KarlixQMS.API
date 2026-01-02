using System.Security.Claims;
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

    // LISTA (dashboard/open actions)
    // GET /api/actions?openOnly=true&overdue=true&type=COMPLAINT&caseNumber=RIN-1234
    [HttpGet]
    public async Task<IActionResult> Get(
        [FromQuery] bool openOnly = true,
        [FromQuery] bool? overdue = null,
        [FromQuery] string? type = null,      // COMPLAINT / NONCONFORMITY
        [FromQuery] string? caseNumber = null,// IssueNumber (RIN-xxxx ili broj za UN)
        [FromQuery] int take = 200)
    {
        var tenantId = _tenant.TenantId;
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        var q = _db.vw_QmsActionOverviews
            .AsNoTracking()
            .Where(x => x.TenantId == tenantId && x.IsActive == true);

        if (openOnly)
            q = q.Where(x => x.CompletedDate == null);

        if (!string.IsNullOrWhiteSpace(type))
            q = q.Where(x => x.EntityType == type);

        // filter po broju slučaja (IssueNumber)
        if (!string.IsNullOrWhiteSpace(caseNumber))
        {
            var pattern = $"%{caseNumber.Trim()}%";
            q = q.Where(x => x.IssueNumber != null && EF.Functions.Like(x.IssueNumber, pattern));
        }

        if (overdue.HasValue)
        {
            if (overdue.Value)
                q = q.Where(x => x.DueDate != null && x.DueDate < today);
            else
                q = q.Where(x => x.DueDate == null || x.DueDate >= today);
        }

        var items = await q
            .OrderBy(x => x.DueDate == null) // non-null prvo
            .ThenBy(x => x.DueDate)
            .Take(Math.Clamp(take, 1, 1000))
            .Select(x => new
            {
                ActionId = x.ActionId, // Guid
                x.IssueNumber,
                x.EntityType,
                x.ActionTitle,
                x.ActionTypeName,
                x.ResponsibleName,
                ResponsibleOrgUnitCode = x.ResponsibleOrgUnitCode,
                ResponsibleOrgUnitName = x.ResponsibleOrgUnitName,
                DueDate = x.DueDate.HasValue ? x.DueDate.Value.ToDateTime(TimeOnly.MinValue) : (DateTime?)null,
                CompletedDate = x.CompletedDate.HasValue ? x.CompletedDate.Value.ToDateTime(TimeOnly.MinValue) : (DateTime?)null,
                StatusCode = (x.CompletedDate == null && x.DueDate != null && x.DueDate < today) ? "OVERDUE" : "IN_PROGRESS",
                DaysLate = (x.CompletedDate == null && x.DueDate != null && x.DueDate < today)
                    ? (today.DayNumber - x.DueDate.Value.DayNumber)
                    : (int?)null
            })
            .ToListAsync();

        return Ok(items);
    }

    // DETAILS
    // GET /api/actions/{id}
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById([FromRoute] Guid id)
    {
        var tenantId = _tenant.TenantId;

        // 1) UI-friendly polja iz view-a
        var v = await _db.vw_QmsActionOverviews
            .AsNoTracking()
            .Where(x => x.TenantId == tenantId && x.ActionId == id && x.IsActive == true)
            .Select(x => new
            {
                x.ActionId,
                Title = x.ActionTitle,
                Description = x.ActionDescription,

                EntityType = x.EntityType,     // COMPLAINT / NONCONFORMITY
                EntityNumber = x.IssueNumber,  // RIN-xxxx / UN-xxxx
                EntityTitle = x.IssueTitle,

                DueDate = x.DueDate.HasValue ? x.DueDate.Value.ToDateTime(TimeOnly.MinValue) : (DateTime?)null,
                CompletedDate = x.CompletedDate.HasValue ? x.CompletedDate.Value.ToDateTime(TimeOnly.MinValue) : (DateTime?)null,

                ResponsibleName = x.ResponsibleName,
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

        if (v == null)
            return NotFound(new { Message = $"Action '{id}' nije pronađena." });

        // 2) ID-jevi za Edit (iz QmsIssueAction tablice)
        var a = await _db.QmsIssueActions
            .AsNoTracking()
            .Where(x => x.TenantId == tenantId && x.Id == id && x.IsActive == true)
            .Select(x => new
            {
                ActionTypeId = (Guid?)x.ActionTypeId,
                EffectivenessId = (Guid?)x.EffectivenessId,
                ResponsibleOrgUnitId = (Guid?)x.ResponsibleOrgUnitId,
                CreatedAt = (DateTime?)x.CreatedAt,
                UpdatedAt = (DateTime?)x.UpdatedAt,
                IsDeleted = (bool?)x.IsDeleted
            })
            .FirstOrDefaultAsync();

        // ako nema reda u tablici, i dalje vrati view podatke
        return Ok(new
        {
            v.ActionId,
            v.Title,
            v.Description,

            v.EntityType,
            v.EntityNumber,
            v.EntityTitle,

            v.DueDate,
            v.CompletedDate,

            v.ResponsibleName,
            v.OrgUnitCode,
            v.OrgUnitName,

            v.ActionTypeCode,
            v.ActionTypeName,

            v.EffectivenessCode,
            v.EffectivenessName,

            v.VerificationDate,
            v.VerificationNotes,

            // ✅ za Edit (dropdown selected)
            ActionTypeId = a?.ActionTypeId,
            EffectivenessId = a?.EffectivenessId,
            ResponsibleOrgUnitId = a?.ResponsibleOrgUnitId,

            // audit/info (ako ima)
            CreatedAt = a?.CreatedAt,
            UpdatedAt = a?.UpdatedAt,
            IsDeleted = a?.IsDeleted
        });
    }

    // UPDATE
    // PUT /api/actions/{id}
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update([FromRoute] Guid id, [FromBody] UpdateActionRequest req)
    {
        var tenantId = _tenant.TenantId;

        if (id == Guid.Empty)
            return BadRequest(new { Message = "Neispravan id." });

        // minimalna validacija (po potrebi proširi)
        if (string.IsNullOrWhiteSpace(req.Title))
            return BadRequest(new { Message = "Title je obavezan." });

        var entity = await _db.QmsIssueActions
            .Where(x => x.TenantId == tenantId && x.Id == id && x.IsActive == true)
            .FirstOrDefaultAsync();

        if (entity == null)
            return NotFound(new { Message = $"Action '{id}' nije pronađena." });

        // update polja
        entity.Title = req.Title?.Trim();
        entity.Description = req.Description;

        // datumi (radi i ako su DateOnly? ili DateTime? u entitetu)
        SetDate(entity, "DueDate", req.DueDate);
        SetDate(entity, "CompletedDate", req.CompletedDate);
        SetDate(entity, "VerificationDate", req.VerificationDate);

        entity.VerificationNotes = req.VerificationNotes;

        // FK / lookups
        entity.ActionTypeId = req.ActionTypeId ?? entity.ActionTypeId;
        entity.EffectivenessId = req.EffectivenessId;

        entity.ResponsibleName = req.ResponsibleName;
        entity.ResponsibleOrgUnitId = req.ResponsibleOrgUnitId;

        // audit
        TrySet(entity, "UpdatedAt", DateTime.UtcNow);
        TrySet(entity, "LastModifiedBy", User?.Identity?.Name ?? User?.FindFirst("name")?.Value);
        TrySet(entity, "LastModifiedAt", DateTime.UtcNow);

        await _db.SaveChangesAsync();

        return NoContent();
    }

    public sealed class UpdateActionRequest
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

    // ---------- helpers (safe set: DateOnly? ili DateTime?) ----------

    private static void SetDate(object target, string propName, DateTime? value)
    {
        var prop = target.GetType().GetProperty(propName);
        if (prop == null || !prop.CanWrite) return;

        var t = Nullable.GetUnderlyingType(prop.PropertyType) ?? prop.PropertyType;

        if (t == typeof(DateOnly))
        {
            var dateOnly = value.HasValue ? DateOnly.FromDateTime(value.Value) : (DateOnly?)null;
            prop.SetValue(target, dateOnly);
            return;
        }

        if (t == typeof(DateTime))
        {
            prop.SetValue(target, value);
            return;
        }

        // ako je neki treći tip, ignoriraj
    }

    private static void TrySet(object target, string propName, object? value)
    {
        var prop = target.GetType().GetProperty(propName);
        if (prop == null || !prop.CanWrite) return;

        try
        {
            prop.SetValue(target, value);
        }
        catch
        {
            // ignore (npr. property ne postoji ili tip mismatch)
        }
    }
}
