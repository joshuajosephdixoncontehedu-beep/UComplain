using System.Text;
using CommunityIncidentReporting.Api.Authorization;
using CommunityIncidentReporting.Api.Filters;
using CommunityIncidentReporting.Api.Middleware;
using CommunityIncidentReporting.Domain.Enums;
using CommunityIncidentReporting.Infrastructure;
using CommunityIncidentReporting.Infrastructure.Security;
using DotNetEnv;
using FluentValidation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Serilog;

// The reporter JWT scheme's name — a distinct ASP.NET Core auth scheme (not just a
// different audience under the default scheme) is what makes Policies.RequireReporter
// able to reject admin tokens outright via AddAuthenticationSchemes, before any claim is
// even inspected.
const string ReporterSchemeName = "ReporterScheme";

// Local development convenience only: if a .env file sits next to this project and no
// ASPNETCORE_ENVIRONMENT is set to something other than Development, load it into the
// process environment so ConnectionStrings__* / Jwt__* / Cors__* variables are picked up
// the same way they would be from real environment variables or Docker/CI secrets.
var environmentName = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development";
var envFilePath = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".env");
if (environmentName == "Development" && File.Exists(envFilePath))
{
    Env.Load(envFilePath);
}

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .MinimumLevel.Override("Microsoft.AspNetCore", Serilog.Events.LogEventLevel.Warning)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    // Render (and most container PaaS hosts) assign a dynamic port via $PORT and expect
    // the app to bind to it — there's no way to know it ahead of time, so it can't be
    // baked into appsettings/launchSettings. Only takes effect when PORT is actually set,
    // so local dev (dotnet run, launchSettings.json) is unaffected.
    var port = Environment.GetEnvironmentVariable("PORT");
    if (!string.IsNullOrEmpty(port))
    {
        builder.WebHost.UseUrls($"http://0.0.0.0:{port}");
    }

    builder.Host.UseSerilog((context, services, configuration) => configuration
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext()
        .WriteTo.Console());

    builder.Services.AddControllers(options => options.Filters.Add<ValidationFilter>())
        .AddJsonOptions(options =>
            options.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter()));

    builder.Services.AddValidatorsFromAssemblyContaining<
        CommunityIncidentReporting.Application.Common.Interfaces.IPasswordHasher>();

    builder.Services.AddInfrastructure(builder.Configuration, builder.Environment);

    builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
    builder.Services.AddProblemDetails();

    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen(options =>
    {
        options.SwaggerDoc("v1", new OpenApiInfo
        {
            Title = "UComplain — Admin API",
            Version = "v1",
            Description = "Admin portal API for UComplain, a WhatsApp-enabled community incident reporting platform. " +
                           "Verified incident reports, verification queues, users, administrators, categories, analytics and audit trails."
        });

        options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
        {
            Name = "Authorization",
            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT",
            In = ParameterLocation.Header,
            Description = "Enter a valid JWT access token issued by /api/admin/auth/login."
        });

        options.AddSecurityRequirement(new OpenApiSecurityRequirement
        {
            {
                new OpenApiSecurityScheme
                {
                    Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
                },
                Array.Empty<string>()
            }
        });
    });

    var allowedOrigins = builder.Configuration
        .GetSection("Cors:AllowedOrigins")
        .Get<string[]>() ?? new[] { "http://localhost:3000" };

    const string CorsPolicyName = "AdminPortalFrontend";
    builder.Services.AddCors(options =>
    {
        options.AddPolicy(CorsPolicyName, policy =>
        {
            policy.WithOrigins(allowedOrigins)
                .AllowAnyHeader()
                .AllowAnyMethod()
                .AllowCredentials();
        });
    });

    builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = builder.Configuration["Jwt:Issuer"],
                ValidateAudience = true,
                ValidAudience = builder.Configuration["Jwt:Audience"],
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(
                    Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Secret"]!)),
                ValidateLifetime = true,
                ClockSkew = TimeSpan.FromMinutes(1)
            };
        })
        // A second, independently-keyed scheme for mobile reporters — its own signing
        // secret and audience (Jwt:ReporterSecret/ReporterAudience) mean an admin token
        // can never pass validation here, and a reporter token can never pass validation
        // against the default scheme above. See ReporterJwtTokenGenerator/Policies.RequireReporter.
        .AddJwtBearer(ReporterSchemeName, options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = builder.Configuration["Jwt:ReporterIssuer"],
                ValidateAudience = true,
                ValidAudience = builder.Configuration["Jwt:ReporterAudience"],
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(
                    Encoding.UTF8.GetBytes(builder.Configuration["Jwt:ReporterSecret"]!)),
                ValidateLifetime = true,
                ClockSkew = TimeSpan.FromMinutes(1)
            };
        });

    builder.Services.AddAuthorizationBuilder()
        .AddPolicy(Policies.SuperAdminOnly, p => p.RequireRole(nameof(AdminRole.SuperAdmin)))
        .AddPolicy(Policies.ManagerOrAbove, p => p.RequireRole(
            nameof(AdminRole.SuperAdmin), nameof(AdminRole.IncidentManager)))
        .AddPolicy(Policies.ReviewerOrAbove, p => p.RequireRole(
            nameof(AdminRole.SuperAdmin), nameof(AdminRole.IncidentManager), nameof(AdminRole.Reviewer)))
        .AddPolicy(Policies.RequireAdmin, p => p
            .AddAuthenticationSchemes(JwtBearerDefaults.AuthenticationScheme)
            .RequireAuthenticatedUser())
        .AddPolicy(Policies.RequireReporter, p => p
            .AddAuthenticationSchemes(ReporterSchemeName)
            .RequireAuthenticatedUser()
            .RequireClaim(System.Security.Claims.ClaimTypes.Role, ReporterJwtTokenGenerator.ReporterRoleClaimValue));

    // Named limiter policies for the mobile auth/OTP endpoints — see
    // MobileAuthController's [EnableRateLimiting] attributes. Partitioned by client IP
    // (X-Forwarded-For is honored below via UseForwardedHeaders) so one abusive caller
    // can't exhaust another's quota; admin endpoints are untouched.
    builder.Services.AddRateLimiter(options =>
    {
        options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

        options.AddPolicy("auth", httpContext => System.Threading.RateLimiting.RateLimitPartition.GetFixedWindowLimiter(
            httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            _ => new System.Threading.RateLimiting.FixedWindowRateLimiterOptions
            {
                Window = TimeSpan.FromMinutes(1),
                PermitLimit = 10,
                QueueLimit = 0
            }));

        options.AddPolicy("otp", httpContext => System.Threading.RateLimiting.RateLimitPartition.GetFixedWindowLimiter(
            httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            _ => new System.Threading.RateLimiting.FixedWindowRateLimiterOptions
            {
                Window = TimeSpan.FromMinutes(1),
                PermitLimit = 5,
                QueueLimit = 0
            }));
    });

    var app = builder.Build();

    // Render (and similar PaaS hosts) terminate TLS at their edge and forward plain HTTP
    // to the container, tagging the original scheme/client IP via X-Forwarded-* headers.
    // Without this, UseHttpsRedirection() below would see every request as HTTP and
    // redirect-loop, and audit log IPs would show the platform's internal proxy instead
    // of the real client. Known-proxy allowlists are cleared because the container never
    // receives traffic except through that one trusted proxy hop — there's no direct path
    // for an external client to spoof these headers.
    var forwardedHeaderOptions = new ForwardedHeadersOptions
    {
        ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
    };
    forwardedHeaderOptions.KnownNetworks.Clear();
    forwardedHeaderOptions.KnownProxies.Clear();
    app.UseForwardedHeaders(forwardedHeaderOptions);

    app.UseExceptionHandler();
    app.UseSerilogRequestLogging();

    // Deliberately not gated to Development: exposed in every environment (including
    // Production on Render) so the live API is self-documenting. It only serves
    // interactive documentation and lets a caller try requests — every endpoint still
    // requires a real JWT the same way it would via curl/Postman, so this doesn't
    // bypass auth or expose secrets, just the API surface itself.
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "UComplain API v1");
        options.DocumentTitle = "UComplain Admin API";
    });

    if (app.Environment.IsDevelopment())
    {
        // dotnet ef database update (and migrations add) always go through
        // DesignTimeDbContextFactory, not this app's DI container — so the
        // UseSeeding/UseAsyncSeeding hooks configured in AddInfrastructure never run
        // via the CLI. Calling Migrate() here, on the real DI-resolved context, applies
        // any pending migrations and runs those seeding hooks. Migrate() is idempotent
        // and the seeder checks for existing data first, so this is safe on every
        // Development startup, not just the first. Stays Development-only — Production
        // (Render) and Testing must never auto-migrate or auto-seed.
        using (var scope = app.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<CommunityIncidentReporting.Infrastructure.Persistence.AppDbContext>();
            db.Database.Migrate();
        }
    }

    app.UseHttpsRedirection();

    app.UseCors(CorsPolicyName);

    app.UseRateLimiter();

    app.UseAuthentication();
    app.UseAuthorization();

    app.MapControllers();

    app.MapGet("/health", () => Results.Ok(new { status = "healthy", service = "CommunityIncidentReporting.Api" }))
        .WithName("HealthCheck")
        .ExcludeFromDescription();

    app.Run();
}
catch (Exception ex) when (ex is not HostAbortedException)
{
    Log.Fatal(ex, "CommunityIncidentReporting.Api terminated unexpectedly during startup");
}
finally
{
    Log.CloseAndFlush();
}

// Exposes the top-level-statement Program for CommunityIncidentReporting.Api.Tests'
// WebApplicationFactory<Program> integration tests.
public partial class Program;
