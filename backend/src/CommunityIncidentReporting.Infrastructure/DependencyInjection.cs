using CommunityIncidentReporting.Application.Common.Interfaces;
using CommunityIncidentReporting.Application.Features.Administrators;
using CommunityIncidentReporting.Application.Features.Auth;
using CommunityIncidentReporting.Application.Features.Analytics;
using CommunityIncidentReporting.Application.Features.AuditLogs;
using CommunityIncidentReporting.Application.Features.Categories;
using CommunityIncidentReporting.Application.Features.Clarifications;
using CommunityIncidentReporting.Application.Features.Dashboard;
using CommunityIncidentReporting.Application.Features.MobileAuth;
using CommunityIncidentReporting.Application.Features.Notifications;
using CommunityIncidentReporting.Application.Features.PublicMap;
using CommunityIncidentReporting.Application.Features.Reporters;
using CommunityIncidentReporting.Application.Features.ReporterAccount;
using CommunityIncidentReporting.Application.Features.Reports;
using CommunityIncidentReporting.Application.Features.Settings;
using CommunityIncidentReporting.Application.Features.Verification;
using CommunityIncidentReporting.Application.Features.Webhooks;
using CommunityIncidentReporting.Application.Features.MobileReports;
using CommunityIncidentReporting.Infrastructure.Compliance;
using CommunityIncidentReporting.Infrastructure.Email;
using CommunityIncidentReporting.Infrastructure.Persistence;
using CommunityIncidentReporting.Infrastructure.Persistence.Seeding;
using CommunityIncidentReporting.Infrastructure.Security;
using CommunityIncidentReporting.Infrastructure.Services;
using CommunityIncidentReporting.Infrastructure.Storage;
using CommunityIncidentReporting.Infrastructure.Verification;
using CommunityIncidentReporting.Infrastructure.WhatsApp;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace CommunityIncidentReporting.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services, IConfiguration configuration, IHostEnvironment environment)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "ConnectionStrings:DefaultConnection is not configured. Set ConnectionStrings__DefaultConnection " +
                "to your Supabase Postgres connection string (see backend/.env.example).");
        }

        services.AddDbContext<AppDbContext>(options =>
        {
            // EnableRetryOnFailure guards against transient network blips to Supabase.
            // Do not combine with a manually started Database.BeginTransaction() —
            // use Database.CreateExecutionStrategy().Execute(...) instead if a future
            // use case needs an explicit multi-SaveChanges transaction.
            options.UseNpgsql(connectionString, npgsql => npgsql.EnableRetryOnFailure(3));

            if (environment.IsDevelopment())
            {
                options.UseSeeding((context, _) =>
                {
                    DevelopmentSeeder.Seed((AppDbContext)context, configuration, new BCryptPasswordHasher());
                });
                options.UseAsyncSeeding(async (context, _, cancellationToken) =>
                {
                    await DevelopmentSeeder.SeedAsync(
                        (AppDbContext)context, configuration, new BCryptPasswordHasher(), cancellationToken);
                });
            }
        });

        services.AddScoped<IPasswordHasher, BCryptPasswordHasher>();

        var jwtSecret = configuration["Jwt:Secret"];
        if (string.IsNullOrWhiteSpace(jwtSecret) || jwtSecret.Length < 32)
        {
            throw new InvalidOperationException(
                "Jwt:Secret is not configured or is shorter than 32 characters. Set Jwt__Secret to a long random " +
                "value (see backend/.env.example) — e.g. `openssl rand -base64 64`.");
        }

        services.Configure<JwtOptions>(configuration.GetSection(JwtOptions.SectionName));
        services.AddScoped<IJwtTokenGenerator, JwtTokenGenerator>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IAuditLogger, AuditLogger>();
        services.AddScoped<ICategoryService, CategoryService>();
        services.AddScoped<IAdministratorService, AdministratorService>();
        services.AddScoped<IReporterService, ReporterService>();
        services.AddScoped<IIncidentReportService, IncidentReportService>();
        services.AddScoped<INotificationService, NotificationService>();
        services.AddScoped<IMobileNotificationService, MobileNotificationService>();
        services.AddScoped<IReportVisibilityService, ReportVisibilityService>();
        services.AddScoped<IPublicMapService, PublicMapService>();

        services.Configure<ClarificationOptions>(configuration.GetSection(ClarificationOptions.SectionName));
        services.AddScoped<IVerificationService, VerificationService>();
        services.AddScoped<IClarificationService, ClarificationService>();
        services.AddScoped<IClarificationAutoCloseService, ClarificationAutoCloseService>();
        services.AddHostedService<ClarificationAutoCloseJob>();
        services.AddScoped<IDashboardService, DashboardService>();
        services.AddScoped<IAnalyticsService, AnalyticsService>();
        services.AddScoped<IAuditLogQueryService, AuditLogQueryService>();
        services.AddScoped<ISettingsService, SettingsService>();

        services.Configure<WhatsAppOptions>(configuration.GetSection(WhatsAppOptions.SectionName));
        services.AddHttpClient();
        services.AddScoped<IWhatsAppWebhookService, WhatsAppWebhookService>();

        // RESEND_API_KEY / RESEND_FROM_EMAIL / RESEND_FROM_NAME / APP_BASE_URL are read
        // as flat top-level keys (not a "Resend:" config section) — see ResendOptions.
        services.Configure<ResendOptions>(o =>
        {
            o.ApiKey = configuration["RESEND_API_KEY"] ?? string.Empty;
            o.FromEmail = configuration["RESEND_FROM_EMAIL"] ?? string.Empty;
            o.FromName = configuration["RESEND_FROM_NAME"] ?? string.Empty;
            o.AppBaseUrl = configuration["APP_BASE_URL"] ?? string.Empty;
        });
        services.AddHttpClient("Resend", client =>
        {
            client.BaseAddress = new Uri("https://api.resend.com/");
            var apiKey = configuration["RESEND_API_KEY"];
            if (!string.IsNullOrWhiteSpace(apiKey))
            {
                client.DefaultRequestHeaders.Authorization =
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", apiKey);
            }
            client.Timeout = TimeSpan.FromSeconds(15);
        });
        services.AddScoped<IEmailService, ResendEmailService>();

        services.Configure<OtpOptions>(configuration.GetSection(OtpOptions.SectionName));
        services.AddScoped<IEmailOtpService, EmailOtpService>();

        var reporterJwtSecret = configuration["Jwt:ReporterSecret"];
        if (string.IsNullOrWhiteSpace(reporterJwtSecret) || reporterJwtSecret.Length < 32)
        {
            throw new InvalidOperationException(
                "Jwt:ReporterSecret is not configured or is shorter than 32 characters. Set " +
                "Jwt__ReporterSecret to a long random value distinct from Jwt__Secret (see backend/.env.example) " +
                "— e.g. `openssl rand -base64 64`.");
        }

        services.Configure<ReporterJwtOptions>(configuration.GetSection(ReporterJwtOptions.SectionName));
        services.AddScoped<IReporterJwtTokenGenerator, ReporterJwtTokenGenerator>();
        services.AddScoped<IReporterAuthService, ReporterAuthService>();

        // SUPABASE_URL / SUPABASE_SERVICE_ROLE_KEY / SUPABASE_STORAGE_BUCKET are flat
        // top-level keys — see SupabaseStorageOptions (same convention as ResendOptions).
        services.Configure<SupabaseStorageOptions>(o =>
        {
            o.Url = configuration["SUPABASE_URL"] ?? string.Empty;
            o.ServiceRoleKey = configuration["SUPABASE_SERVICE_ROLE_KEY"] ?? string.Empty;
            o.StorageBucket = configuration["SUPABASE_STORAGE_BUCKET"] ?? string.Empty;
        });
        services.AddHttpClient("Supabase", client =>
        {
            var supabaseUrl = configuration["SUPABASE_URL"];
            if (!string.IsNullOrWhiteSpace(supabaseUrl))
            {
                client.BaseAddress = new Uri($"{supabaseUrl.TrimEnd('/')}/storage/v1/");
            }

            var serviceRoleKey = configuration["SUPABASE_SERVICE_ROLE_KEY"];
            if (!string.IsNullOrWhiteSpace(serviceRoleKey))
            {
                client.DefaultRequestHeaders.Authorization =
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", serviceRoleKey);
                client.DefaultRequestHeaders.Add("apikey", serviceRoleKey);
            }
            client.Timeout = TimeSpan.FromSeconds(60);
        });
        services.AddScoped<ISupabaseStorageService, SupabaseStorageService>();

        services.Configure<MediaUploadOptions>(configuration.GetSection(MediaUploadOptions.SectionName));
        services.AddScoped<IMobileReportService, MobileReportService>();
        services.AddScoped<IMediaAttachmentService, MediaAttachmentService>();

        services.Configure<ComplianceOptions>(configuration.GetSection(ComplianceOptions.SectionName));
        services.AddScoped<IReporterAccountService, ReporterAccountService>();
        services.AddScoped<IReporterAnonymizationService, ReporterAnonymizationService>();
        services.AddScoped<IDataExportProcessorService, DataExportProcessorService>();
        services.AddHostedService<DataExportJob>();
        services.AddScoped<IAccountDeletionProcessorService, AccountDeletionProcessorService>();
        services.AddHostedService<AccountDeletionJob>();
        services.AddScoped<IReporterRetentionPurgeService, ReporterRetentionPurgeService>();
        services.AddHostedService<ReporterRetentionPurgeJob>();

        return services;
    }
}
