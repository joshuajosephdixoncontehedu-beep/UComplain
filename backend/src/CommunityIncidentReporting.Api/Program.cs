using DotNetEnv;
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

    builder.Services.AddControllers();

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

    // Authentication/authorization scheme configuration (JWT bearer + role policies) is added in Phase 2.
    builder.Services.AddAuthentication();
    builder.Services.AddAuthorization();

    var app = builder.Build();

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
