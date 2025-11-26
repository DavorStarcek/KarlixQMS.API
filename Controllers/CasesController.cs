using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OpenIddict.Validation.AspNetCore;
using System.Security.Claims;

namespace KarlixQMS.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)]
    public class CasesController : ControllerBase
    {
        [HttpGet]
        public IActionResult Get()
        {
            // Tko zove API?
            var email =
                User.FindFirst(ClaimTypes.Email)?.Value ??
                User.Identity?.Name ??
                "(unknown)";

            // Tenant claim (Guid) iz tokena
            var tenantRaw = User.FindFirst("tenant")?.Value;

            string tenantLabel;
            if (string.IsNullOrWhiteSpace(tenantRaw) || tenantRaw == Guid.Empty.ToString())
            {
                tenantLabel = "Global";
            }
            else
            {
                // kasnije ovdje možeš mapirati GUID → naziv tenanta iz baze
                tenantLabel = tenantRaw;
            }

            var data = new[]
            {
                new CaseDto
                {
                    Id = 1,
                    Type = "CustomerComplaint",
                    Title = "Reklamacija kupca #1",
                    Status = "Open",
                    Tenant = tenantLabel,
                    CreatedBy = email
                },
                new CaseDto
                {
                    Id = 2,
                    Type = "InternalNonconformity",
                    Title = "Interna nesukladnost #1",
                    Status = "InProgress",
                    Tenant = tenantLabel,
                    CreatedBy = email
                }
            };

            return Ok(data);
        }
    }

    public class CaseDto
    {
        public int Id { get; set; }
        public string? Type { get; set; }
        public string? Title { get; set; }
        public string? Status { get; set; }
        public string? Tenant { get; set; }
        public string? CreatedBy { get; set; }
    }
}
