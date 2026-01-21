using KarlixQMS.API.Data;
using KarlixQMS.API.Infrastructure;
using KarlixQMS.API.Infrastructure.Security;
using KarlixQMS.API.Models.Tables;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OpenIddict.Validation.AspNetCore;
using System.Security.Claims;
using System.Text.Json;

namespace KarlixQMS.API.Controllers;

[ApiController]
[Route("api/cases")]
[Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)]
public class CaseFieldsController : ControllerBase
{
    private readonly QmsDbContext _db;
    private readonly ITenantContext _tenant;

    public CaseFieldsController(QmsDbContext db, ITenantContext tenant)
    {
        _db = db;
        _tenant = tenant;
    }

    // PATCH /api/cases/{number}/fields
    [HttpPatch("{number}/fields")]
    [Authorize(Policy = QmsPolicies.CasesRead)] // minimalno mora imati read, a write rješavamo ručno (po fazi)
    public async Task<IActionResult> PatchFields([FromRoute] string number, [FromBody] JsonElement body)
    {
        if (string.IsNullOrWhiteSpace(number))
            return BadRequest(new { Message = "Number is required." });

        if (body.ValueKind != JsonValueKind.Object)
            return BadRequest(new { Message = "Body must be a JSON object." });

        var tenantId = _tenant.TenantId;
        number = number.Trim();

        // 1) Nađi case header preko view-a (da dobijemo EntityType + WorkflowStatusId + StatusCode)
        var header = await _db.vw_QmsIssueLists
            .AsNoTracking()
            .Where(x => x.TenantId == tenantId && x.Number == number)
            .Select(x => new
            {
                x.EntityId,
                x.EntityType,
                x.WorkflowStatusId,
                x.StatusCode
            })
            .FirstOrDefaultAsync();

        if (header == null)
            return NotFound(new { Message = $"Case '{number}' nije pronađen." });

        var entityType = (header.EntityType ?? "").Trim().ToUpperInvariant();
        if (entityType != "COMPLAINT" && entityType != "NONCONFORMITY")
            return BadRequest(new { Message = $"Unsupported EntityType '{header.EntityType}'." });

        // 2) Odredi fazu (na temelju workflow statusa)
        // Pravilo:
        // - RECEIVED: IsInitial == true
        // - CLOSED: IsFinal == true
        // - IN_PROGRESS: sve ostalo (nije initial i nije final i nije cancelled)
        var ws = await _db.QmsWorkflowStatuses
            .AsNoTracking()
            .Where(x => x.TenantId == tenantId && x.Id == header.WorkflowStatusId && x.IsActive == true)
            .Select(x => new { x.IsInitial, x.IsFinal, x.IsCancelled, x.Code })
            .FirstOrDefaultAsync();

        if (ws == null)
            return BadRequest(new { Message = "Workflow status nije pronađen ili nije aktivan." });

        var phase =
            ws.IsFinal ? "CLOSED" :
            ws.IsInitial ? "RECEIVED" :
            "IN_PROGRESS";

        // 3) Permission check (TenantAdmin/GlobalAdmin bypass)
        if (!UserIsTenantOrGlobalAdmin())
        {
            var requiredPerm = RequiredPerm(entityType, phase);
            if (!UserHasPerm(requiredPerm) && !UserHasPerm(QmsPerms.QmsAdmin))
                return Forbid();
        }

        // 4) Parse patch keys
        var patch = ParsePatch(body);

        if (patch.Count == 0)
            return BadRequest(new { Message = "No fields provided." });

        // 5) Whitelist polja po entitetu
        if (entityType == "COMPLAINT")
        {
            // RIN: dozvoljena polja po tabovima/fazama (simple whitelist)
            // RECEIVED
            var allowedReceived = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "title", "description", "orgUnitId"
            };

            // IN_PROGRESS
            var allowedInProgress = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "analysis", "rootCause", "immediateCorrection"
            };

            // CLOSED
            var allowedClosed = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "closedDate", "isComplaintJustified", "feedbackToCustomer"
            };

            var allowed = phase switch
            {
                "RECEIVED" => allowedReceived,
                "IN_PROGRESS" => allowedInProgress,
                "CLOSED" => allowedClosed,
                _ => allowedInProgress
            };

            var unknown = patch.Keys.Where(k => !allowed.Contains(k)).ToList();
            if (unknown.Any())
                return BadRequest(new { Message = $"Field(s) not allowed for RIN/{phase}: {string.Join(", ", unknown)}" });

            // 6) Load entity
            var complaint = await _db.QmsCustomerComplaints
                .FirstOrDefaultAsync(x => x.TenantId == tenantId && x.Number == number);

            if (complaint == null)
                return NotFound(new { Message = $"Complaint '{number}' nije pronađena." });

            // 7) Apply
            ApplyComplaintPatch(complaint, patch);

            complaint.LastModifiedAt = DateTime.UtcNow;
            complaint.LastModifiedBy = User.Identity?.Name;

            await _db.SaveChangesAsync();
            return NoContent();
        }
        else // NONCONFORMITY
        {
            // UN: whitelist po fazi
            var allowedReceived = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "title", "description", "orgUnitId"
            };

            var allowedInProgress = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "rootCause", "cause", "immediateCorrection", "correctiveAction", "preventiveAction", "dueDate"
            };

            var allowedClosed = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "closeDate", "verificationDate", "verifiedBy", "effectivenessId", "effectivenessComment"
            };

            var allowed = phase switch
            {
                "RECEIVED" => allowedReceived,
                "IN_PROGRESS" => allowedInProgress,
                "CLOSED" => allowedClosed,
                _ => allowedInProgress
            };

            var unknown = patch.Keys.Where(k => !allowed.Contains(k)).ToList();
            if (unknown.Any())
                return BadRequest(new { Message = $"Field(s) not allowed for UN/{phase}: {string.Join(", ", unknown)}" });

            var nc = await _db.QmsNonconformities
                .FirstOrDefaultAsync(x => x.TenantId == tenantId && x.Number == number);

            if (nc == null)
                return NotFound(new { Message = $"Nonconformity '{number}' nije pronađena." });

            ApplyNonconformityPatch(nc, patch);

            nc.LastModifiedAt = DateTime.UtcNow;
            nc.LastModifiedBy = User.Identity?.Name;

            await _db.SaveChangesAsync();
            return NoContent();
        }
    }

    // -----------------------
    // Helpers
    // -----------------------

    private bool UserIsTenantOrGlobalAdmin()
        => User.IsInRole(AppRoles.GlobalAdmin) || User.IsInRole(AppRoles.TenantAdmin);

    private bool UserHasPerm(string perm)
        => User.Claims.Any(c => c.Type == "perm" && string.Equals(c.Value, perm, StringComparison.OrdinalIgnoreCase));

    private static string RequiredPerm(string entityType, string phase)
    {
        // entityType: COMPLAINT (RIN) / NONCONFORMITY (UN)
        if (entityType == "COMPLAINT")
        {
            return phase switch
            {
                "RECEIVED" => QmsPerms.RinWriteReceived,
                "IN_PROGRESS" => QmsPerms.RinWriteInProgress,
                "CLOSED" => QmsPerms.RinWriteClosed,
                _ => QmsPerms.RinWriteInProgress
            };
        }

        return phase switch
        {
            "RECEIVED" => QmsPerms.UnWriteReceived,
            "IN_PROGRESS" => QmsPerms.UnWriteInProgress,
            "CLOSED" => QmsPerms.UnWriteClosed,
            _ => QmsPerms.UnWriteInProgress
        };
    }

    private static Dictionary<string, JsonElement> ParsePatch(JsonElement obj)
    {
        var dict = new Dictionary<string, JsonElement>(StringComparer.OrdinalIgnoreCase);
        foreach (var p in obj.EnumerateObject())
            dict[p.Name] = p.Value;
        return dict;
    }

    private static string? GetStringOrNull(JsonElement v)
    {
        if (v.ValueKind == JsonValueKind.Null) return null;
        if (v.ValueKind == JsonValueKind.String) return v.GetString();
        return v.ToString();
    }

    private static bool? GetBoolOrNull(JsonElement v)
    {
        if (v.ValueKind == JsonValueKind.Null) return null;
        if (v.ValueKind == JsonValueKind.True) return true;
        if (v.ValueKind == JsonValueKind.False) return false;
        if (v.ValueKind == JsonValueKind.String && bool.TryParse(v.GetString(), out var b)) return b;
        return null;
    }

    private static Guid? GetGuidOrNull(JsonElement v)
    {
        if (v.ValueKind == JsonValueKind.Null) return null;
        if (v.ValueKind == JsonValueKind.String && Guid.TryParse(v.GetString(), out var g)) return g;
        if (Guid.TryParse(v.ToString(), out var g2)) return g2;
        return null;
    }

    private static DateOnly? GetDateOnlyOrNull(JsonElement v)
    {
        if (v.ValueKind == JsonValueKind.Null) return null;

        if (v.ValueKind == JsonValueKind.String)
        {
            var s = v.GetString();
            if (string.IsNullOrWhiteSpace(s)) return null;

            // očekujemo yyyy-MM-dd
            if (DateTime.TryParse(s, out var dt))
                return DateOnly.FromDateTime(dt);
        }

        // fallback
        if (DateTime.TryParse(v.ToString(), out var dt2))
            return DateOnly.FromDateTime(dt2);

        return null;
    }

    private static void ApplyComplaintPatch(QmsCustomerComplaint c, Dictionary<string, JsonElement> patch)
    {
        foreach (var (key, v) in patch)
        {
            switch (key.ToLowerInvariant())
            {
                case "title":
                    c.Title = GetStringOrNull(v);
                    break;

                case "description":
                    c.Description = GetStringOrNull(v);
                    break;

                case "orgunitid":
                    c.OrgUnitId = GetGuidOrNull(v);
                    break;

                case "analysis":
                    c.Analysis = GetStringOrNull(v);
                    break;

                case "rootcause":
                    c.RootCause = GetStringOrNull(v);
                    break;

                case "immediatecorrection":
                    c.ImmediateCorrection = GetStringOrNull(v);
                    break;

                case "closeddate":
                    c.ClosedDate = GetDateOnlyOrNull(v);
                    break;

                case "iscomplaintjustified":
                    c.IsComplaintJustified = GetBoolOrNull(v);
                    break;

                case "feedbacktocustomer":
                    c.FeedbackToCustomer = GetStringOrNull(v);
                    break;
            }
        }
    }

    private static void ApplyNonconformityPatch(QmsNonconformity n, Dictionary<string, JsonElement> patch)
    {
        foreach (var (key, v) in patch)
        {
            switch (key.ToLowerInvariant())
            {
                case "title":
                    if (GetStringOrNull(v) is string s1)
                        n.Title = s1;
                    else
                        n.Title = ""; // Title je NOT NULL u tvojoj tablici
                    break;

                case "description":
                    n.Description = GetStringOrNull(v);
                    break;

                case "orgunitid":
                    n.OrgUnitId = GetGuidOrNull(v);
                    break;

                case "rootcause":
                    n.RootCause = GetStringOrNull(v);
                    break;

                case "cause":
                    n.Cause = GetStringOrNull(v);
                    break;

                case "immediatecorrection":
                    n.ImmediateCorrection = GetStringOrNull(v);
                    break;

                case "correctiveaction":
                    n.CorrectiveAction = GetStringOrNull(v);
                    break;

                case "preventiveaction":
                    n.PreventiveAction = GetStringOrNull(v);
                    break;

                case "duedate":
                    n.DueDate = GetDateOnlyOrNull(v);
                    break;

                case "closedate":
                    n.CloseDate = GetDateOnlyOrNull(v);
                    break;

                case "verificationdate":
                    n.VerificationDate = GetDateOnlyOrNull(v);
                    break;

                case "verifiedby":
                    n.VerifiedBy = GetStringOrNull(v);
                    break;

                case "effectivenessid":
                    n.EffectivenessId = GetGuidOrNull(v);
                    break;

                case "effectivenesscomment":
                    n.EffectivenessComment = GetStringOrNull(v);
                    break;
            }
        }
    }

    // Lokalni “roles” stringovi da ne ovisimo o KarlixID projektu
    private static class AppRoles
    {
        public const string GlobalAdmin = "GlobalAdmin";
        public const string TenantAdmin = "TenantAdmin";
    }
}
