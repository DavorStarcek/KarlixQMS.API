using Microsoft.AspNetCore.Authentication;
using Microsoft.OpenApi.Models;
using OpenIddict.Validation.AspNetCore;

var builder = WebApplication.CreateBuilder(args);
var env = builder.Environment;
var isDev = env.IsDevelopment();

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
        Description = "Unesi JWT token. Primjer: Bearer eyJhbGciOi...",
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

// =========================
// OpenIddict validation – QMS.Api vjeruje KarlixID-u
// =========================
builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme;
});

builder.Services.AddOpenIddict()
    .AddValidation(options =>
    {
        options.SetIssuer(isDev
            ? "https://localhost:7173/"
            : "https://id.karlix.eu/");

        options.UseSystemNetHttp();
        options.UseAspNetCore();
    });

var app = builder.Build();

if (isDev)
{
    app.UseDeveloperExceptionPage();
}

app.UseHttpsRedirection();

// Swagger UI na rootu
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Karlix QMS API v1");
    c.RoutePrefix = string.Empty; // https://host/ → swagger
});

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
