using KarlixQMS.API.Data;
using KarlixQMS.API.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OpenIddict.Validation.AspNetCore;

namespace KarlixQMS.API.Controllers;

[ApiController]
[Route("api/lookups")]
[Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)]
public class LookupsController : ControllerBase
{
    private readonly QmsDbContext _db;
    private readonly ITenantContext _tenant;

    public LookupsController(QmsDbContext db, ITenantContext tenant)
    {
        _db = db;
        _tenant = tenant;
    }

    [HttpGet("action-types")]
    public async Task<IActionResult> GetActionTypes()
    {
        var tenantId = _tenant.TenantId;

        var items = await _db.QmsActionTypes
            .AsNoTracking()
            .Where(x => x.TenantId == tenantId && x.IsActive == true)
            .OrderBy(x => x.DisplayOrder)
            .ThenBy(x => x.Name)
            .Select(x => new
            {
                x.Id,
                x.Code,
                x.Name
            })
            .ToListAsync();

        return Ok(items);
    }

    [HttpGet("effectiveness")]
    public async Task<IActionResult> GetEffectiveness()
    {
        var tenantId = _tenant.TenantId;

        var items = await _db.QmsEffectivenesses
            .AsNoTracking()
            .Where(x => x.TenantId == tenantId && x.IsActive == true)
            .OrderBy(x => x.DisplayOrder)
            .ThenBy(x => x.Name)
            .Select(x => new
            {
                x.Id,
                x.Code,
                x.Name
            })
            .ToListAsync();

        return Ok(items);
    }

    [HttpGet("org-units")]
    public async Task<IActionResult> GetOrgUnits()
    {
        var tenantId = _tenant.TenantId;

        var items = await _db.QmsOrgUnits
            .AsNoTracking()
            .Where(x => x.TenantId == tenantId && x.IsActive == true)
            .OrderBy(x => x.DisplayOrder)
            .ThenBy(x => x.Name)
            .Select(x => new
            {
                x.Id,
                x.Code,
                x.Name
            })
            .ToListAsync();

        return Ok(items);
    }
}
