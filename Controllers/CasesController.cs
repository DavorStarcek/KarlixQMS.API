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

    // ----------------------------
    // DTO
    // ----------------------------
    public sealed class CaseStatusUpdateDto
    {
        public Guid WorkflowStatusId { get; set; }
    }

    private bool IsAdmin() =>
        User.IsInRole("GlobalAdmin") || User.IsInRole("TenantAdmin");

    private bool HasPerm(string perm) =>
        User.HasClaim("perm", perm);

    private static string? NormalizeEntityType(string? t)
    {
        t = string.IsNullOrWhiteSpace(t) ? null : t.Trim().ToUpperInvariant();
        if (t is "COMPLAINT" or "NONCONFORMITY") return t;
        return t; // može biti null ili nešto drugo, ali mi očekujemo COMPLAINT/NONCONFORMITY iz view-a
    }

    private static string? NormalizeStatusCode(string? code)
    {
        if (string.IsNullOrWhiteSpace(code)) return null;
        return code.Trim().ToUpperInvariant();
    }

    private static string? BuildPhasePerm(string entityType, string statusCode)
    {
        entityType = NormalizeEntityType(entityType) ?? entityType;
        statusCode = NormalizeStatusCode(statusCode) ?? statusCode;

        // Po tvom modelu: perm ovisi o trenutnoj fazi (statusCode)
        return entityType switch
        {
            "COMPLAINT" => $"qms.rin.write.{statusCode}",
            "NONCONFORMITY" => $"qms.un.write.{statusCode}",
            _ => null
        };
    }

    private async Task<(bool ok, string? error)> EnsureCanWriteThisCaseAsync(vw_QmsIssueList header)
    {
        if (IsAdmin())
            return (true, null);

        // Ako nema status code-a u view-u, ne možemo fazno odlučiti → sigurnije zabrani.
        // (Ako želiš fallback na qms.admin, možemo i to, ali trenutno admini već prolaze preko role-a.)
        var entityType = NormalizeEntityType(header.EntityType);
        var statusCode = NormalizeStatusCode(header.StatusCode);

        if (string.IsNullOrWhiteSpace(entityType) || string.IsNullOrWhiteSpace(statusCode))
            return (false, "Slučaj nema EntityType/StatusCode (view). Ne mogu provjeriti fazna prava.");

        var needed = BuildPhasePerm(entityType!, statusCode!);
        if (string.IsNullOrWhiteSpace(needed))
            return (false, "Nepoznat EntityType za fazna prava.");

        if (!HasPerm(needed))
            return (false, $"Nedovoljna prava. Potrebno: '{needed}'.");

        return (true, null);
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
    // (ostavljam kako ti radi – ovdje ga ne diram)
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

        // (Ovdje vraćaš Actions kako već imaš u svom trenutnom kodu)
        // Ne diram to sad.
        return Ok(header);
    }

    // ============================================================
    // STATUS UPDATE
    // PUT /api/cases/{number}/status
    // ============================================================
    [HttpPut("{number}/status")]
    [Authorize(Policy = QmsPolicies.CasesWriteBasic)]
    public async Task<IActionResult> UpdateStatus([FromRoute] string number, [FromBody] CaseStatusUpdateDto dto)
    {
        if (string.IsNullOrWhiteSpace(number))
            return BadRequest(new { Message = "Number is required." });

        if (dto.WorkflowStatusId == Guid.Empty)
            return BadRequest(new { Message = "WorkflowStatusId is required." });

        var tenantId = _tenant.TenantId;
        number = number.Trim();

        // 1) Učitaj header iz view-a (imamo EntityType, EntityId, WorkflowStatusId, StatusCode...)
        var header = await _db.vw_QmsIssueLists
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.TenantId == tenantId && x.Number == number);

        if (header == null)
            return NotFound(new { Message = $"Case '{number}' nije pronađen." });

        // 2) Fazna provjera prava po TRENUTNOM statusu slučaja
        var (ok, err) = await EnsureCanWriteThisCaseAsync(header);
        if (!ok)
            return Forbid(); // standardno 403; poruku možeš logirati ako želiš

        // 3) Validiraj da je target status aktivan i za isti EntityType i tenant
        var entityType = NormalizeEntityType(header.EntityType) ?? header.EntityType ?? "";
        if (string.IsNullOrWhiteSpace(entityType))
            return BadRequest(new { Message = "Case EntityType is missing." });

        var targetStatus = await _db.QmsWorkflowStatuses
            .AsNoTracking()
            .FirstOrDefaultAsync(ws =>
                ws.Id == dto.WorkflowStatusId &&
                ws.IsActive == true &&
                ws.TenantId == tenantId &&
                ws.EntityType == entityType);

        if (targetStatus == null)
            return BadRequest(new { Message = "Neispravan WorkflowStatusId (nije aktivan / nije za taj tenant / nije za taj EntityType)." });

        // 4) Update: (ne diramo još druga polja; ovo je univerzalno i stabilno)
        if (entityType == "COMPLAINT")
        {
            // QmsCustomerComplaint ima WorkflowStatusId (po tvojoj shemi)
            var rin = await _db.QmsCustomerComplaints
                .FirstOrDefaultAsync(x => x.TenantId == tenantId && x.Id == header.EntityId);

            if (rin == null)
                return NotFound(new { Message = "RIN entity nije pronađen." });

            rin.WorkflowStatusId = dto.WorkflowStatusId;
        }
        else if (entityType == "NONCONFORMITY")
        {
            // QmsNonconformity ima WorkflowStatusId (po tvojoj shemi)
            var un = await _db.QmsNonconformities
                .FirstOrDefaultAsync(x => x.TenantId == tenantId && x.Id == header.EntityId);

            if (un == null)
                return NotFound(new { Message = "UN entity nije pronađen." });

            un.WorkflowStatusId = dto.WorkflowStatusId;
        }
        else
        {
            return BadRequest(new { Message = $"Nepodržan EntityType '{entityType}'." });
        }

        await _db.SaveChangesAsync();
        return NoContent();
    }
}
