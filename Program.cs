using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using OpenIddict.Validation.AspNetCore;

using KarlixQMS.API.Data;
using KarlixQMS.API.Infrastructure;
using KarlixQMS.API.Infrastructure.Security;

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

builder.Services.AddAuthorization(options =>
{
    static bool IsAdmin(Microsoft.AspNetCore.Authorization.AuthorizationHandlerContext ctx) =>
        ctx.User.IsInRole("GlobalAdmin") || ctx.User.IsInRole("TenantAdmin");

    static bool HasPerm(Microsoft.AspNetCore.Authorization.AuthorizationHandlerContext ctx, string perm) =>
        ctx.User.HasClaim("perm", perm);

    static bool HasPermPrefix(Microsoft.AspNetCore.Authorization.AuthorizationHandlerContext ctx, string prefix) =>
        ctx.User.Claims.Any(c =>
            string.Equals(c.Type, "perm", StringComparison.OrdinalIgnoreCase) &&
            !string.IsNullOrWhiteSpace(c.Value) &&
            c.Value.StartsWith(prefix, StringComparison.OrdinalIgnoreCase));

    // -------------------------
    // ACTIONS
    // -------------------------
    options.AddPolicy(QmsPolicies.ActionsRead, p =>
        p.RequireAssertion(ctx => IsAdmin(ctx) || HasPerm(ctx, QmsPerms.ActionsRead)));

    options.AddPolicy(QmsPolicies.ActionsWriteBasic, p =>
        p.RequireAssertion(ctx => IsAdmin(ctx) || HasPerm(ctx, QmsPerms.ActionsWriteBasic)));

    options.AddPolicy(QmsPolicies.ActionsVerify, p =>
        p.RequireAssertion(ctx => IsAdmin(ctx) || HasPerm(ctx, QmsPerms.ActionsVerify)));

    // -------------------------
    // CASES
    // -------------------------
    // Čitanje slučajeva = qms.read (ili admin)
    options.AddPolicy(QmsPolicies.CasesRead, p =>
        p.RequireAssertion(ctx => IsAdmin(ctx) || HasPerm(ctx, QmsPerms.Read) || HasPerm(ctx, QmsPerms.Admin)));

    // “Gate” za write na slučajevima:
    // Admin uvijek može, a ostali ako imaju neki phase write perm (rin/un), ili qms.admin.
    // Pravu faznu kontrolu radimo u Cases controlleru (EntityType+StatusCode -> konkretan perm).
    options.AddPolicy(QmsPolicies.CasesWriteBasic, p =>
        p.RequireAssertion(ctx =>
            IsAdmin(ctx) ||
            HasPerm(ctx, QmsPerms.Admin) ||
            HasPermPrefix(ctx, "qms.rin.write.") ||
            HasPermPrefix(ctx, "qms.un.write.")));
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
