using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using OpenIddict.Validation.AspNetCore;

using KarlixQMS.API.Data;
using KarlixQMS.API.Infrastructure;
using KarlixQMS.API.Infrastructure.Security;
using Microsoft.AspNetCore.Authorization;

var builder = WebApplication.CreateBuilder(args);

var env = builder.Environment;
var isDev = env.IsDevelopment();

// dodatno: flag za detaljne greške (čita se iz env var)
var detailedErrorsEnv = Environment.GetEnvironmentVariable("ASPNETCORE_DETAILEDERRORS");
var showDevErrors =
    isDev ||
    string.Equals(detailedErrorsEnv, "true", StringComparison.OrdinalIgnoreCase);

//
// =========================
// Services
// =========================
//

// Controllers
builder.Services.AddControllers();

// Swagger + Bearer auth
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Karlix QMS API",
        Version = "v1"
    });

    var securityScheme = new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Description = "Bearer {access_token}",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT"
    };

    c.AddSecurityDefinition("Bearer", securityScheme);

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

// HttpContext + Tenant
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ITenantContext, TenantContext>();

// EF Core – QMS baza
builder.Services.AddDbContext<QmsDbContext>(options =>
{
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        sql => sql.EnableRetryOnFailure());
});

//
// =========================
// Authentication / Authorization
// =========================
//

// Authority (KarlixID)
var authority =
    builder.Configuration.GetSection("Authentication")["Authority"]
    ?? "https://localhost:7173";

builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme =
        OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme;
});

builder.Services.AddOpenIddict()
    .AddValidation(options =>
    {
        options.SetIssuer(authority.TrimEnd('/') + "/");
        options.UseSystemNetHttp();
        options.UseAspNetCore();
    });

// ✅ Permission-based authorization (perm claim)
builder.Services.AddSingleton<IAuthorizationHandler, PermissionAuthorizationHandler>();

builder.Services.AddAuthorization(options =>
{
    // Baseline (read)
    options.AddPolicy(QmsPolicies.QmsRead, p =>
    {
        p.RequireAuthenticatedUser();
        p.AddRequirements(new PermissionRequirement(
            QmsPermissions.Read,
            QmsPermissions.Admin
        ));
    });

    // Actions read
    options.AddPolicy(QmsPolicies.QmsActionsRead, p =>
    {
        p.RequireAuthenticatedUser();
        p.AddRequirements(new PermissionRequirement(
            QmsPermissions.ActionsRead,
            QmsPermissions.Read,
            QmsPermissions.Admin
        ));
    });

    // Actions write basic
    options.AddPolicy(QmsPolicies.QmsActionsWriteBasic, p =>
    {
        p.RequireAuthenticatedUser();
        p.AddRequirements(new PermissionRequirement(
            QmsPermissions.ActionsWriteBasic,
            QmsPermissions.ActionsWriteAll,
            QmsPermissions.Admin
        ));
    });

    // Actions verify
    options.AddPolicy(QmsPolicies.QmsActionsVerify, p =>
    {
        p.RequireAuthenticatedUser();
        p.AddRequirements(new PermissionRequirement(
            QmsPermissions.ActionsVerify,
            QmsPermissions.ActionsWriteAll,
            QmsPermissions.Admin
        ));
    });

    // Actions write all
    options.AddPolicy(QmsPolicies.QmsActionsWriteAll, p =>
    {
        p.RequireAuthenticatedUser();
        p.AddRequirements(new PermissionRequirement(
            QmsPermissions.ActionsWriteAll,
            QmsPermissions.Admin
        ));
    });
});

//
// =========================
// App
// =========================
//

var app = builder.Build();

//
// Pipeline
//
if (showDevErrors)
{
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler("/error");
    app.UseHsts();
}

app.UseHttpsRedirection();

// Swagger uvijek dostupan (DEV + PROD)
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Karlix QMS API v1");
    c.RoutePrefix = string.Empty;
});

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// Minimal error endpoint
app.Map("/error", () =>
{
    return Results.Problem("Došlo je do greške prilikom obrade zahtjeva.");
});

app.Run();
