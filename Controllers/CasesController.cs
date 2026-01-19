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

    // ----------------------------
    // DTOs
    // ----------------------------
    public sealed class CaseListItemDto
    {
        public Guid EntityId { get; set; }
        public string Number { get; set; } = null!;
        public string? EntityType { get; set; }
        public string? Title { get; set; }

        public DateTime IssueDate { get; set; }
        public DateTime? ReceivedDate { get; set; }

        public string? CustomerName { get; set; }
        public Guid? OrgUnitId { get; set; }

        public string? StatusCode { get; set; }
        public string? StatusName { get; set; }
    }

    public sealed class CaseDetailsDto
    {
        // header
        public Guid EntityId { get; set; }
        public string Number { get; set; } = null!;
        public string? EntityType { get; set; }
        public string? Title { get; set; }

        public DateTime IssueDate { get; set; }
        public DateTime? ReceivedDate { get; set; }

        public string? CustomerName { get; set; }
        public Guid? OrgUnitId { get; set; }

        public Guid WorkflowStatusId { get; set; }
        public string? StatusCode { get; set; }
        public string? StatusName { get; set; }

        // actions (iz vw_QmsIssue_Actions)
        public List<CaseActionDto> Actions { get; set; } = new();
    }

    public sealed class CaseActionDto
    {
        public Guid ActionId { get; set; }
        public string? ActionTitle { get; set; }
        public string? ActionTypeName { get; set; }
        public string? ResponsibleName { get; set; }

        public DateTime? DueDate { get; set; }
        public DateTime? CompletedDate { get; set; }
        public DateTime? VerificationDate { get; set; }

        public string? EffectivenessCode { get; set; }
        public string? EffectivenessName { get; set; }
    }

    // ----------------------------
    // LIST
    // GET /api/cases?type=COMPLAINT&status=OPEN&q=rin&take=200
    // ----------------------------
    [HttpGet]
    public async Task<ActionResult<List<CaseListItemDto>>> Get(
        [FromQuery] string? type = null,     // COMPLAINT / NONCONFORMITY
        [FromQuery] string? status = null,   // StatusCode iz vw
        [FromQuery] string? q = null,        // search
        [FromQuery] int take = 200)
    {
        var tenantId = _tenant.TenantId;

        var query = _db.vw_QmsIssueLists
            .AsNoTracking()
            .Where(x => x.TenantId == tenantId);

        if (!string.IsNullOrWhiteSpace(type))
            query = query.Where(x => x.EntityType == type);

        if (!string.IsNullOrWhiteSpace(status))
            query = query.Where(x => x.StatusCode == status);

        if (!string.IsNullOrWhiteSpace(q))
        {
            var term = q.Trim();
            var pattern = $"%{term}%";
            query = query.Where(x =>
                EF.Functions.Like(x.Number, pattern) ||
                (x.Title != null && EF.Functions.Like(x.Title, pattern)) ||
                (x.CustomerName != null && EF.Functions.Like(x.CustomerName, pattern)));
        }

        var items = await query
            .OrderByDescending(x => x.IssueDate)
            .ThenBy(x => x.Number)
            .Take(Math.Clamp(take, 1, 1000))
            .Select(x => new CaseListItemDto
            {
                EntityId = x.EntityId,
                Number = x.Number,
                EntityType = x.EntityType,
                Title = x.Title,

                IssueDate = x.IssueDate.ToDateTime(TimeOnly.MinValue),
                ReceivedDate = x.ReceivedDate.HasValue ? x.ReceivedDate.Value.ToDateTime(TimeOnly.MinValue) : (DateTime?)null,

                CustomerName = x.CustomerName,
                OrgUnitId = x.OrgUnitId,

                StatusCode = x.StatusCode,
                StatusName = x.StatusName
            })
            .ToListAsync();

        return Ok(items);
    }

    // ----------------------------
    // DETAILS by number
    // GET /api/cases/{number}
    // ----------------------------
    [HttpGet("{number}")]
    public async Task<ActionResult<CaseDetailsDto>> GetByNumber([FromRoute] string number)
    {
        var tenantId = _tenant.TenantId;
        var n = (number ?? "").Trim();

        if (string.IsNullOrWhiteSpace(n))
            return BadRequest(new { Message = "Number je obavezan." });

        var header = await _db.vw_QmsIssueLists
            .AsNoTracking()
            .Where(x => x.TenantId == tenantId && x.Number == n)
            .Select(x => new CaseDetailsDto
            {
                EntityId = x.EntityId,
                Number = x.Number,
                EntityType = x.EntityType,
                Title = x.Title,

                IssueDate = x.IssueDate.ToDateTime(TimeOnly.MinValue),
                ReceivedDate = x.ReceivedDate.HasValue ? x.ReceivedDate.Value.ToDateTime(TimeOnly.MinValue) : (DateTime?)null,

                CustomerName = x.CustomerName,
                OrgUnitId = x.OrgUnitId,

                WorkflowStatusId = x.WorkflowStatusId,
                StatusCode = x.StatusCode,
                StatusName = x.StatusName
            })
            .FirstOrDefaultAsync();

        if (header == null)
            return NotFound(new { Message = $"Case '{n}' nije pronađen." });

        // actions list (vw_QmsIssue_Actions) - filtriramo po IssueNumber
        // Napomena: vw_QmsIssue_Action entity ima polje IssueNumber (vidi tvoj OnModelCreating).
        var actions = await _db.vw_QmsIssue_Actions
            .AsNoTracking()
            .Where(a => a.TenantId == tenantId && a.IssueNumber == n)
            .OrderBy(a => a.CompletedDate == null) // otvorene prve
            .ThenBy(a => a.DueDate)
            .Select(a => new CaseActionDto
            {
                ActionId = a.Id,
                ActionTitle = a.ActionTitle,
                ActionTypeName = a.ActionTypeName,
                ResponsibleName = a.ResponsibleName,

                DueDate = a.DueDate.HasValue ? a.DueDate.Value.ToDateTime(TimeOnly.MinValue) : (DateTime?)null,
                CompletedDate = a.CompletedDate.HasValue ? a.CompletedDate.Value.ToDateTime(TimeOnly.MinValue) : (DateTime?)null,
                VerificationDate = a.VerificationDate.HasValue ? a.VerificationDate.Value.ToDateTime(TimeOnly.MinValue) : (DateTime?)null,

                EffectivenessCode = a.EffectivenessCode,
                EffectivenessName = a.EffectivenessName
            })
            .ToListAsync();

        header.Actions = actions;
        return Ok(header);
    }
}
