using System.Text;
using CommunityIncidentReporting.Api.Authorization;
using CommunityIncidentReporting.Api.Filters;
using CommunityIncidentReporting.Api.Middleware;
using CommunityIncidentReporting.Domain.Enums;
using CommunityIncidentReporting.Infrastructure;
using DotNetEnv;
using FluentValidation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Serilog;

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
            Title = "Community Incident Reporting System — Admin API",
            Version = "v1",
            Description = "Admin portal API for the WhatsApp-enabled Community Incident Reporting System. " +
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
        });

    builder.Services.AddAuthorizationBuilder()
        .AddPolicy(Policies.SuperAdminOnly, p => p.RequireRole(nameof(AdminRole.SuperAdmin)))
        .AddPolicy(Policies.ManagerOrAbove, p => p.RequireRole(
            nameof(AdminRole.SuperAdmin), nameof(AdminRole.IncidentManager)))
        .AddPolicy(Policies.ReviewerOrAbove, p => p.RequireRole(
            nameof(AdminRole.SuperAdmin), nameof(AdminRole.IncidentManager), nameof(AdminRole.Reviewer)));

    var app = builder.Build();

    app.UseExceptionHandler();
    app.UseSerilogRequestLogging();

    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI(options =>
        {
            options.SwaggerEndpoint("/swagger/v1/swagger.json", "Community Incident Reporting System API v1");
            options.DocumentTitle = "CIRS Admin API";
        });
    }

    app.UseHttpsRedirection();

    app.UseCors(CorsPolicyName);

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
