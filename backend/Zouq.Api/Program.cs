using System.Security.Claims;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Zouq.Api.Filters;
using Zouq.Application.Interfaces;
using Zouq.Infrastructure.Data;
using Zouq.Infrastructure.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<ZouqDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddHttpClient("zouq-ai");
builder.Services.AddScoped<IJwtTokenService, JwtTokenService>();
builder.Services.AddScoped<IPasswordHasher, BcryptPasswordHasher>();
builder.Services.AddScoped<IFileStorageService, LocalFileStorageService>();
builder.Services.AddScoped<IAiImageAnalysisService, ConfigurableAiImageAnalysisService>();
builder.Services.AddSingleton<AiUploadProcessor>();
builder.Services.AddScoped<IPriceCalculationService, PriceCalculationService>();
builder.Services.AddScoped<DesignIntegrityService>();
builder.Services.AddScoped<FinancialLedgerService>();
builder.Services.AddScoped<AuthAppService>();
builder.Services.AddScoped<CatalogAppService>();
builder.Services.AddScoped<DesignAppService>();
builder.Services.AddScoped<OrderAppService>();
builder.Services.AddScoped<FeedAppService>();

var corsOrigins = builder.Configuration.GetSection("Cors:Origins").Get<string[]>()
    ?? Array.Empty<string>();
builder.Services.AddCors(options =>
{
    options.AddPolicy("ZouqCors", policy =>
    {
        if (builder.Environment.IsDevelopment() && corsOrigins.Length == 0)
        {
            policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader();
            return;
        }

        if (corsOrigins.Length == 0)
            throw new InvalidOperationException("Cors:Origins must be configured outside Development.");

        policy.WithOrigins(corsOrigins).AllowAnyMethod().AllowAnyHeader();
    });
});

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy("auth", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            httpContext.Connection.RemoteIpAddress?.ToString() ?? "anon",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 20,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0
            }));
    options.AddPolicy("upload", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            httpContext.User.FindFirstValue("id") ?? httpContext.Connection.RemoteIpAddress?.ToString() ?? "anon",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 30,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0
            }));
    options.AddPolicy("mutate", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            httpContext.User.FindFirstValue("id") ?? httpContext.Connection.RemoteIpAddress?.ToString() ?? "anon",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 60,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0
            }));
    options.AddPolicy("feed", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            httpContext.Connection.RemoteIpAddress?.ToString() ?? "anon",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 120,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0
            }));
});

builder.Services.AddControllers(options => options.Filters.Add<ApiExceptionFilter>())
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower;
        options.JsonSerializerOptions.DictionaryKeyPolicy = JsonNamingPolicy.SnakeCaseLower;
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });

var jwtSettings = builder.Configuration.GetSection("Jwt");
var keyStr = jwtSettings["Key"];
if (string.IsNullOrWhiteSpace(keyStr) ||
    keyStr.Contains("ChangeMe", StringComparison.OrdinalIgnoreCase))
{
    if (builder.Environment.IsProduction())
        throw new InvalidOperationException("Jwt:Key must be set to a strong secret in Production (do not use ChangeMe defaults).");
}

keyStr ??= "Zouq_Dev_Only_Insecure_Key_Do_Not_Use_In_Production_2026!";
var key = Encoding.UTF8.GetBytes(keyStr);
if (key.Length < 32)
    key = System.Security.Cryptography.SHA256.HashData(Encoding.UTF8.GetBytes(keyStr));

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.RequireHttpsMetadata = !builder.Environment.IsDevelopment();
        options.SaveToken = true;
        options.MapInboundClaims = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(key),
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidIssuer = jwtSettings["Issuer"],
            ValidAudience = jwtSettings["Audience"],
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromMinutes(1),
            RoleClaimType = "role",
            NameClaimType = "unique_name"
        };
        options.Events = new JwtBearerEvents
        {
            OnTokenValidated = ctx =>
            {
                var identity = ctx.Principal?.Identity as ClaimsIdentity;
                if (identity == null) return Task.CompletedTask;
                var role = identity.FindFirst("role")?.Value ?? identity.FindFirst(ClaimTypes.Role)?.Value;
                if (!string.IsNullOrWhiteSpace(role) && !identity.HasClaim(ClaimTypes.Role, role))
                    identity.AddClaim(new Claim(ClaimTypes.Role, role));
                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorization();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c => c.SwaggerDoc("v1", new() { Title = "Zouq API", Version = "v1" }));

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("ZouqCors");
app.UseRateLimiter();
app.UseStaticFiles();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

try
{
    await ZouqSeeder.SeedAsync(app.Services);
}
catch (Exception ex)
{
    app.Logger.LogWarning(ex, "Database seed skipped — check connection string.");
}

app.Run();
