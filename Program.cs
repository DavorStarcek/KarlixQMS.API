using Microsoft.AspNetCore.Authentication;
using Microsoft.OpenApi.Models;
using OpenIddict.Validation.AspNetCore;
using Microsoft.EntityFrameworkCore;
using KarlixQMS.API.Data;
using KarlixQMS.API.Infrastructure;


var builder = WebApplication.CreateBuilder(args);

var env = builder.Environment;
var isDev = env.IsDevelopment();

// dodatno: flag za detaljne greške (čita se iz env var)
var detailedErrorsEnv = Environment.GetEnvironmentVariable("ASPNETCORE_DETAILEDERRORS");
var showDevErrors =
    isDev ||
    string.Equals(detailedErrorsEnv, "true", StringComparison.OrdinalIgnoreCase);

// =========================
// MVC API + Swagger
// =========================
builder.Services.AddControllers();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Karlix QMS API",
        Version = "v1"
    });

    // 🔐 Bearer auth za Swagger (ručni unos tokena)
    var securityScheme = new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Description = "Unesi JWT token. Primjer: Bearer eyJhbGciOiJSUzI1NiIsInR5cCI6IkpXVCJ9...",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT"
    };

    c.AddSecurityDefinition("Bearer", securityScheme);

    var securityRequirement = new OpenApiSecurityRequirement
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
    };

    c.AddSecurityRequirement(securityRequirement);
});


builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ITenantContext, TenantContext>();

builder.Services.AddDbContext<QmsDbContext>(opt =>
{
    opt.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"));
});





// =========================
// OpenIddict validation – QMS.Api vjeruje KarlixID-u
// =========================

// authority čitamo iz appsettings.* (DEV i PROD varijante)
var authSection = builder.Configuration.GetSection("Authentication");
var authority = authSection["Authority"] ?? "https://localhost:7173";

builder.Services.AddAuthentication(options =>
{
    // default schema je OpenIddict validation
    options.DefaultScheme = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme;
});

builder.Services.AddOpenIddict()
    .AddValidation(options =>
    {
        // npr. https://localhost:7173/ ili https://id.karlix.eu/
        options.SetIssuer(authority.TrimEnd('/') + "/");

        // QMS.Api će pitati KarlixID za public keys / metadata
        options.UseSystemNetHttp();
        options.UseAspNetCore();
    });

var app = builder.Build();

// ========= Pipeline =========

if (showDevErrors)
{
    // DEV ili ASPNETCORE_DETAILEDERRORS=true → full stack trace u browseru
    app.UseDeveloperExceptionPage();
}
else
{
    // klasični production handling (možemo dodati /error endpoint)
    app.UseExceptionHandler("/error");
    app.UseHsts();
}

app.UseHttpsRedirection();

// Swagger UI na rootu (radi i u DEV i u PROD)
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

// Minimalni /error endpoint – u prod vraća generičku poruku
app.Map("/error", (HttpContext httpContext) =>
{
    return Results.Problem("Došlo je do greške prilikom obrade zahtjeva.");
});

app.Run();
