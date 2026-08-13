using CommunityIncidentReporting.Application.Common.Interfaces;
using CommunityIncidentReporting.Domain.Entities;
using CommunityIncidentReporting.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace CommunityIncidentReporting.Infrastructure.Persistence.Seeding;

/// <summary>
/// Development-only seed data: one SuperAdmin, an admin per role, incident categories,
/// fictional Sierra Leonean reporters and reports spanning every status, plus the
/// verification events, notes, assignments, and audit logs that go with them.
///
/// Runs via EF Core's UseSeeding/UseAsyncSeeding hooks (invoked automatically by
/// `dotnet ef database update` and Database.Migrate()) rather than migration HasData,
/// because the admin passwords must be BCrypt-hashed at runtime from configuration —
/// not baked into a migration as a static value.
/// </summary>
public static class DevelopmentSeeder
{
    public static void Seed(AppDbContext context, IConfiguration configuration, IPasswordHasher passwordHasher)
    {
        if (context.AdminUsers.Any())
        {
            return;
        }

        var seedPassword = configuration["SeedData:SuperAdminPassword"];
        if (string.IsNullOrWhiteSpace(seedPassword))
        {
            throw new InvalidOperationException(
                "SeedData:SuperAdminPassword is not configured. Set it via backend/src/CommunityIncidentReporting.Api/.env " +
                "(SeedData__SuperAdminPassword) before seeding a Development database. It is reused for every seeded " +
                "demo administrator account.");
        }

        var passwordHash = passwordHasher.Hash(seedPassword);
        var now = DateTimeOffset.UtcNow;

        var (admins, superAdmin, incidentManager, reviewer, analyst) = BuildAdmins(passwordHash, now);
        context.AdminUsers.AddRange(admins);

        var categories = BuildCategories(now);
        context.IncidentCategories.AddRange(categories);

        var reporters = BuildReporters(now);
        context.Reporters.AddRange(reporters);

        var reports = BuildReports(now, categories, reporters, incidentManager, reviewer);
        context.IncidentReports.AddRange(reports);

        var auditLogs = BuildAuditLogs(now, superAdmin, incidentManager, reviewer, reports);
        context.AuditLogs.AddRange(auditLogs);

        context.SaveChanges();
    }

    public static async Task SeedAsync(
        AppDbContext context,
        IConfiguration configuration,
        IPasswordHasher passwordHasher,
        CancellationToken cancellationToken)
    {
        if (await context.AdminUsers.AnyAsync(cancellationToken))
        {
            return;
        }

        Seed(context, configuration, passwordHasher);
        await Task.CompletedTask;
    }

    private static (List<AdminUser> All, AdminUser SuperAdmin, AdminUser IncidentManager, AdminUser Reviewer, AdminUser Analyst)
        BuildAdmins(string passwordHash, DateTimeOffset now)
    {
        var superAdmin = new AdminUser
        {
            Id = Guid.NewGuid(),
            FullName = "Aminata Kargbo",
            Email = "aminata.kargbo@cirs.gov.sl",
            PasswordHash = passwordHash,
            Role = AdminRole.SuperAdmin,
            IsActive = true,
            CreatedAt = now.AddMonths(-6),
            UpdatedAt = now.AddMonths(-6)
        };

        var incidentManager = new AdminUser
        {
            Id = Guid.NewGuid(),
            FullName = "Mohamed Sesay",
            Email = "mohamed.sesay@cirs.gov.sl",
            PasswordHash = passwordHash,
            Role = AdminRole.IncidentManager,
            IsActive = true,
            CreatedAt = now.AddMonths(-5),
            UpdatedAt = now.AddMonths(-5)
        };

        var reviewer = new AdminUser
        {
            Id = Guid.NewGuid(),
            FullName = "Fatmata Koroma",
            Email = "fatmata.koroma@cirs.gov.sl",
            PasswordHash = passwordHash,
            Role = AdminRole.Reviewer,
            IsActive = true,
            CreatedAt = now.AddMonths(-4),
            UpdatedAt = now.AddMonths(-4)
        };

        var analyst = new AdminUser
        {
            Id = Guid.NewGuid(),
            FullName = "Ibrahim Turay",
            Email = "ibrahim.turay@cirs.gov.sl",
            PasswordHash = passwordHash,
            Role = AdminRole.ReadOnlyAnalyst,
            IsActive = true,
            CreatedAt = now.AddMonths(-3),
            UpdatedAt = now.AddMonths(-3)
        };

        return ([superAdmin, incidentManager, reviewer, analyst], superAdmin, incidentManager, reviewer, analyst);
    }

    private static List<IncidentCategory> BuildCategories(DateTimeOffset now)
    {
        (string Name, string Description, IncidentPriority Priority, int SlaHours, int Order)[] defs =
        [
            ("Flooding", "Flash floods, drainage overflow, and flood-related property damage.", IncidentPriority.High, 12, 1),
            ("Fire Outbreak", "Structure fires, market fires, and bushfires near residential areas.", IncidentPriority.Critical, 4, 2),
            ("Road Traffic Accident", "Collisions, road obstructions, and traffic hazards.", IncidentPriority.High, 6, 3),
            ("Public Health Concern", "Suspected disease outbreaks, contaminated water sources, sanitation hazards.", IncidentPriority.High, 12, 4),
            ("Infrastructure Damage", "Damaged roads, bridges, public buildings, or utility poles.", IncidentPriority.Medium, 48, 5),
            ("Security Incident", "Theft, assault, or other public safety and security concerns.", IncidentPriority.Critical, 6, 6),
            ("Water Supply Disruption", "Broken pipes, prolonged outages, or unsafe water supply.", IncidentPriority.Medium, 24, 7),
            ("Power Outage", "Extended electricity outages affecting a neighborhood or facility.", IncidentPriority.Low, 72, 8)
        ];

        return defs.Select(d => new IncidentCategory
        {
            Id = Guid.NewGuid(),
            Name = d.Name,
            Description = d.Description,
            DefaultPriority = d.Priority,
            SlaHours = d.SlaHours,
            IsActive = true,
            DisplayOrder = d.Order,
            CreatedAt = now.AddMonths(-6),
            UpdatedAt = now.AddMonths(-6)
        }).ToList();
    }

    private static List<Reporter> BuildReporters(DateTimeOffset now)
    {
        (string Hash, string Masked, VerificationStatus Status, int ConsentDaysAgo, bool Restricted)[] defs =
        [
            ("wa_hash_7a1c9e2f4b6d8091", "+232 76 ***  214", VerificationStatus.Verified, 180, false),
            ("wa_hash_3f8b2d7a1c5e9042", "+232 77 ***  558", VerificationStatus.Verified, 150, false),
            ("wa_hash_9d4e1a6c8f2b3075", "+232 78 ***  391", VerificationStatus.Verified, 120, false),
            ("wa_hash_2c7f9b3e5a1d6084", "+232 76 ***  742", VerificationStatus.Verified, 90, false),
            ("wa_hash_5a9c3f7b1e2d4066", "+232 79 ***  103", VerificationStatus.Pending, 10, false),
            ("wa_hash_8e2d6a4c9f1b3057", "+232 77 ***  876", VerificationStatus.Verified, 60, false),
            ("wa_hash_1b6f4d8a3c7e9028", "+232 78 ***  429", VerificationStatus.FlaggedAbuse, 45, true),
            ("wa_hash_4d8a2c6f9b3e1073", "+232 76 ***  615", VerificationStatus.Verified, 200, false)
        ];

        return defs.Select(d => new Reporter
        {
            Id = Guid.NewGuid(),
            WhatsAppNumberHash = d.Hash,
            MaskedContactReference = d.Masked,
            VerificationStatus = d.Status,
            ConsentAt = now.AddDays(-d.ConsentDaysAgo),
            IsRestricted = d.Restricted,
            CreatedAt = now.AddDays(-d.ConsentDaysAgo),
            UpdatedAt = now.AddDays(-d.ConsentDaysAgo)
        }).ToList();
    }

    private static List<IncidentReport> BuildReports(
        DateTimeOffset now,
        List<IncidentCategory> categories,
        List<Reporter> reporters,
        AdminUser incidentManager,
        AdminUser reviewer)
    {
        IncidentCategory Cat(string name) => categories.First(c => c.Name == name);

        var reports = new List<IncidentReport>();

        // 1. Verified, resolved — flooding in Kroo Bay with full lifecycle history.
        reports.Add(BuildResolvedReport(now, Cat("Flooding"), reporters[0], incidentManager,
            "Kroo Bay, Freetown", -8.0142, -13.2412,
            "Heavy rain has flooded the main footpath and several homes near the stream in Kroo Bay; " +
            "residents report waist-deep water overnight.",
            "Community drainage volunteers cleared the blocked culvert and the water level receded within " +
            "48 hours; two affected households received temporary shelter support from the ward councillor."));

        // 2. Verified, in progress, assigned, critical fire.
        reports.Add(BuildAssignedInProgressReport(now, Cat("Fire Outbreak"), reporters[1], incidentManager,
            "Congo Cross Market, Freetown", -8.4869, -13.2459,
            "Fire broke out at the eastern row of market stalls around 6pm; several traders' stock destroyed, " +
            "smoke visible from Congo Cross roundabout."));

        // 3. Verified, under review (not yet assigned), road traffic accident.
        reports.Add(BuildUnderReviewReport(now, Cat("Road Traffic Accident"), reporters[2],
            "Wellington Road, Freetown", -8.4732, -13.1859,
            "Two vehicles collided near the Wellington roundabout, partially blocking the eastbound lane; " +
            "one motorcyclist reported injured."));

        // 4. Verified, resolved, public health.
        reports.Add(BuildResolvedReport(now, Cat("Public Health Concern"), reporters[3], reviewer,
            "Susan's Bay, Freetown", -8.4880, -13.2338,
            "Several households report stomach illness after using the community well; residents suspect " +
            "contamination from the nearby latrine.",
            "Ministry of Health water team tested and chlorinated the well; illness cases stopped within a week."));

        // 5. Pending verification — newly received, awaiting review.
        reports.Add(BuildPendingVerificationReport(now, Cat("Infrastructure Damage"), reporters[4],
            "Regent Road, Freetown", -8.4599, -13.2076,
            "A retaining wall along Regent Road has developed a large crack after last week's rain and looks " +
            "close to collapse onto the road below."));

        // 6. Needs clarification — vague location.
        reports.Add(BuildVerificationQueueReport(now, Cat("Security Incident"), reporters[5], reviewer,
            "Near Lumley Beach, Freetown", null, null,
            "There was a robbery near the beach last night, not sure exactly where, someone should check.",
            VerificationStatus.NeedsClarification, VerificationMethod.AdminReview, VerificationDecisionResult.ClarificationRequested,
            "Reporter did not provide a specific location or time; requested more detail via WhatsApp follow-up."));

        // 7. Suspected duplicate.
        reports.Add(BuildVerificationQueueReport(now, Cat("Water Supply Disruption"), reporters[0],
            reviewer, "Aberdeen, Freetown", -8.4869, -13.2822,
            "No water from the standpipe on Aberdeen Road for the third day in a row.",
            VerificationStatus.SuspectedDuplicate, VerificationMethod.AutomatedDuplicateCheck, VerificationDecisionResult.MarkedDuplicate,
            "Matches an existing verified report for the same standpipe outage filed two days earlier."));

        // 8. Flagged abuse — restricted reporter.
        reports.Add(BuildVerificationQueueReport(now, Cat("Security Incident"), reporters[6],
            incidentManager, "Kissy, Freetown", null, null,
            "This is the fifth report from this number this week with no verifiable details; pattern suggests misuse.",
            VerificationStatus.FlaggedAbuse, VerificationMethod.AdminReview, VerificationDecisionResult.Escalated,
            "Escalated for review given repeated low-detail submissions from a restricted reporter."));

        // 9. Rejected.
        reports.Add(BuildVerificationQueueReport(now, Cat("Power Outage"), reporters[7],
            reviewer, "Kenema Town", null, null,
            "Power has been out for one hour, please help.",
            VerificationStatus.Rejected, VerificationMethod.AdminReview, VerificationDecisionResult.Rejected,
            "Does not meet the reporting threshold — routine short outage already scheduled for maintenance."));

        // 10. Verified, closed, low priority infrastructure.
        reports.Add(BuildClosedReport(now, Cat("Infrastructure Damage"), reporters[3], incidentManager,
            "Hill Station, Freetown", -8.4550, -13.2280,
            "Streetlight pole leaning dangerously over the footpath near Hill Station junction.",
            "Pole was straightened and secured by the utility company; case closed after confirmation visit."));

        // 11. Verified, assigned, medium priority, awaiting action.
        reports.Add(BuildAssignedReport(now, Cat("Water Supply Disruption"), reporters[2], reviewer,
            "Waterloo, Freetown", -8.3389, -13.0725,
            "Main pipeline burst near Waterloo junction, flooding the road and cutting supply to nearby streets."));

        // 12. Verified, under review, high priority, unassigned.
        reports.Add(BuildUnderReviewReport(now, Cat("Fire Outbreak"), reporters[1],
            "Bo Town, Southern Province", -7.9647, -11.7383,
            "Bushfire spreading close to farmland on the outskirts of Bo, smoke visible from the highway."));

        return reports;
    }

    private static IncidentReport NewReport(
        DateTimeOffset now,
        IncidentCategory category,
        Reporter reporter,
        string location,
        double? lat,
        double? lng,
        string description,
        int daysAgo)
    {
        var createdAt = now.AddDays(-daysAgo);
        return new IncidentReport
        {
            Id = Guid.NewGuid(),
            Reporter = reporter,
            Category = category,
            SourceChannel = SourceChannel.WhatsApp,
            Description = description,
            IncidentOccurredAt = createdAt.AddHours(-2),
            LocationDescription = location,
            Latitude = lat,
            Longitude = lng,
            Priority = category.DefaultPriority,
            CreatedAt = createdAt,
            UpdatedAt = createdAt
        };
    }

    private static IncidentReport BuildResolvedReport(
        DateTimeOffset now, IncidentCategory category, Reporter reporter, AdminUser admin,
        string location, double lat, double lng, string description, string resolution)
    {
        var report = NewReport(now, category, reporter, location, lat, lng, description, daysAgo: 20);
        report.VerificationStatus = VerificationStatus.Verified;
        report.CaseStatus = CaseStatus.Resolved;
        report.AssignedAdmin = admin;
        report.ResolutionSummary = resolution;
        report.UpdatedAt = now.AddDays(-2);

        AddVerificationEvent(report, reporter, admin, VerificationDecisionResult.Approved,
            "Location and description corroborated with a follow-up WhatsApp photo.", daysAgo: 19);
        AddAssignment(report, admin, admin, daysAgo: 18);
        AddStatusStep(report, admin, CaseStatus.VerificationPending, CaseStatus.UnderReview, daysAgo: 19, notes: null);
        AddStatusStep(report, admin, CaseStatus.UnderReview, CaseStatus.Assigned, daysAgo: 18, notes: "Assigned for field follow-up.");
        AddStatusStep(report, admin, CaseStatus.Assigned, CaseStatus.InProgress, daysAgo: 17, notes: "Ward team notified.");
        AddStatusStep(report, admin, CaseStatus.InProgress, CaseStatus.Resolved, daysAgo: 2, notes: resolution);
        AddNote(report, admin, "Confirmed resolution with community focal point by phone.", daysAgo: 2);

        return report;
    }

    private static IncidentReport BuildClosedReport(
        DateTimeOffset now, IncidentCategory category, Reporter reporter, AdminUser admin,
        string location, double lat, double lng, string description, string resolution)
    {
        var report = BuildResolvedReport(now, category, reporter, admin, location, lat, lng, description, resolution);
        report.CaseStatus = CaseStatus.Closed;
        report.ClosedAt = now.AddDays(-1);
        report.UpdatedAt = now.AddDays(-1);
        AddStatusStep(report, admin, CaseStatus.Resolved, CaseStatus.Closed, daysAgo: 1, notes: "Closed after 48-hour confirmation window.");
        return report;
    }

    private static IncidentReport BuildAssignedInProgressReport(
        DateTimeOffset now, IncidentCategory category, Reporter reporter, AdminUser admin,
        string location, double lat, double lng, string description)
    {
        var report = NewReport(now, category, reporter, location, lat, lng, description, daysAgo: 2);
        report.VerificationStatus = VerificationStatus.Verified;
        report.CaseStatus = CaseStatus.InProgress;
        report.AssignedAdmin = admin;
        report.Priority = IncidentPriority.Critical;
        report.UpdatedAt = now.AddHours(-6);

        AddVerificationEvent(report, reporter, admin, VerificationDecisionResult.Approved,
            "Corroborated by two independent WhatsApp reports from the same market area.", daysAgo: 2);
        AddAssignment(report, admin, admin, daysAgo: 2);
        AddStatusStep(report, admin, CaseStatus.VerificationPending, CaseStatus.Assigned, daysAgo: 2, notes: "Fire — assigned immediately.");
        AddStatusStep(report, admin, CaseStatus.Assigned, CaseStatus.InProgress, daysAgo: 1, notes: "Fire service and ward disaster team dispatched.");
        AddNote(report, admin, "Coordinating with the National Fire Force liaison for an update.", daysAgo: 0);

        return report;
    }

    private static IncidentReport BuildAssignedReport(
        DateTimeOffset now, IncidentCategory category, Reporter reporter, AdminUser admin,
        string location, double lat, double lng, string description)
    {
        var report = NewReport(now, category, reporter, location, lat, lng, description, daysAgo: 3);
        report.VerificationStatus = VerificationStatus.Verified;
        report.CaseStatus = CaseStatus.Assigned;
        report.AssignedAdmin = admin;
        report.UpdatedAt = now.AddDays(-1);

        AddVerificationEvent(report, reporter, admin, VerificationDecisionResult.Approved,
            "Confirmed via a short follow-up call with the reporter.", daysAgo: 3);
        AddAssignment(report, admin, admin, daysAgo: 1);
        AddStatusStep(report, admin, CaseStatus.VerificationPending, CaseStatus.UnderReview, daysAgo: 3, notes: null);
        AddStatusStep(report, admin, CaseStatus.UnderReview, CaseStatus.Assigned, daysAgo: 1, notes: "Assigned to utility liaison.");

        return report;
    }

    private static IncidentReport BuildUnderReviewReport(
        DateTimeOffset now, IncidentCategory category, Reporter reporter,
        string location, double lat, double lng, string description)
    {
        var report = NewReport(now, category, reporter, location, lat, lng, description, daysAgo: 1);
        report.VerificationStatus = VerificationStatus.Verified;
        report.CaseStatus = CaseStatus.UnderReview;

        AddVerificationEvent(report, reporter, null, VerificationDecisionResult.Approved,
            "Details consistent with location and time reported.", daysAgo: 1);
        AddStatusStep(report, null, CaseStatus.VerificationPending, CaseStatus.UnderReview, daysAgo: 1, notes: null, actorRequired: false);

        return report;
    }

    private static IncidentReport BuildPendingVerificationReport(
        DateTimeOffset now, IncidentCategory category, Reporter reporter,
        string location, double lat, double lng, string description)
    {
        var report = NewReport(now, category, reporter, location, lat, lng, description, daysAgo: 0);
        report.VerificationStatus = VerificationStatus.Pending;
        report.CaseStatus = CaseStatus.VerificationPending;
        return report;
    }

    private static IncidentReport BuildVerificationQueueReport(
        DateTimeOffset now, IncidentCategory category, Reporter reporter, AdminUser reviewingAdmin,
        string location, double? lat, double? lng, string description,
        VerificationStatus verificationStatus, VerificationMethod method, VerificationDecisionResult result,
        string decisionNotes)
    {
        var report = NewReport(now, category, reporter, location, lat, lng, description, daysAgo: 4);
        report.VerificationStatus = verificationStatus;
        report.CaseStatus = CaseStatus.VerificationPending;
        report.UpdatedAt = now.AddDays(-3);

        AddVerificationEvent(report, reporter, reviewingAdmin, result, decisionNotes, daysAgo: 3, method: method);

        if (verificationStatus == VerificationStatus.Rejected)
        {
            report.CaseStatus = CaseStatus.Rejected;
        }
        else if (verificationStatus == VerificationStatus.SuspectedDuplicate)
        {
            report.CaseStatus = CaseStatus.Duplicate;
        }

        return report;
    }

    private static void AddVerificationEvent(
        IncidentReport report, Reporter reporter, AdminUser? admin, VerificationDecisionResult result,
        string notes, int daysAgo, VerificationMethod method = VerificationMethod.AdminReview)
    {
        report.VerificationEvents.Add(new VerificationEvent
        {
            Id = Guid.NewGuid(),
            IncidentReport = report,
            Reporter = reporter,
            VerificationMethod = method,
            Result = result,
            AttemptNumber = report.VerificationEvents.Count + 1,
            Notes = notes,
            PerformedByAdmin = admin,
            CreatedAt = DateTimeOffset.UtcNow.AddDays(-daysAgo)
        });
    }

    private static void AddAssignment(IncidentReport report, AdminUser admin, AdminUser assignedBy, int daysAgo)
    {
        report.ReportAssignments.Add(new ReportAssignment
        {
            Id = Guid.NewGuid(),
            IncidentReport = report,
            AdminUser = admin,
            AssignedByAdmin = assignedBy,
            AssignedAt = DateTimeOffset.UtcNow.AddDays(-daysAgo)
        });
    }

    private static void AddStatusStep(
        IncidentReport report, AdminUser? admin, CaseStatus previous, CaseStatus next, int daysAgo, string? notes,
        bool actorRequired = true)
    {
        // StatusHistory.ChangedByAdminId is required; system-driven transitions (e.g. automatic
        // verification-queue placement) attribute to the report's assigned/reviewing admin when
        // one is available, since there is no "system" admin account in this schema.
        var actor = admin ?? report.AssignedAdmin;
        if (actor is null)
        {
            if (actorRequired)
            {
                throw new InvalidOperationException("A StatusHistory entry requires an attributable admin.");
            }

            return;
        }

        report.StatusHistories.Add(new StatusHistory
        {
            Id = Guid.NewGuid(),
            IncidentReport = report,
            PreviousStatus = previous,
            NewStatus = next,
            ChangedByAdmin = actor,
            Notes = notes,
            CreatedAt = DateTimeOffset.UtcNow.AddDays(-daysAgo)
        });
    }

    private static void AddNote(IncidentReport report, AdminUser admin, string content, int daysAgo)
    {
        var createdAt = DateTimeOffset.UtcNow.AddDays(-daysAgo);
        report.InternalNotes.Add(new InternalNote
        {
            Id = Guid.NewGuid(),
            IncidentReport = report,
            Content = content,
            CreatedByAdmin = admin,
            CreatedAt = createdAt,
            UpdatedAt = createdAt
        });
    }

    private static List<AuditLog> BuildAuditLogs(
        DateTimeOffset now, AdminUser superAdmin, AdminUser incidentManager, AdminUser reviewer,
        List<IncidentReport> reports)
    {
        var logs = new List<AuditLog>
        {
            new()
            {
                Id = Guid.NewGuid(),
                AdminUser = superAdmin,
                Action = "AdministratorCreated",
                EntityType = nameof(AdminUser),
                EntityId = incidentManager.Id.ToString(),
                NewValueJson = $$"""{"fullName":"{{incidentManager.FullName}}","role":"IncidentManager"}""",
                CreatedAt = now.AddMonths(-5)
            },
            new()
            {
                Id = Guid.NewGuid(),
                AdminUser = superAdmin,
                Action = "AdministratorCreated",
                EntityType = nameof(AdminUser),
                EntityId = reviewer.Id.ToString(),
                NewValueJson = $$"""{"fullName":"{{reviewer.FullName}}","role":"Reviewer"}""",
                CreatedAt = now.AddMonths(-4)
            }
        };

        foreach (var report in reports.Where(r => r.VerificationEvents.Count > 0))
        {
            var lastEvent = report.VerificationEvents.OrderBy(e => e.CreatedAt).Last();
            logs.Add(new AuditLog
            {
                Id = Guid.NewGuid(),
                AdminUser = lastEvent.PerformedByAdmin,
                Action = "VerificationDecisionRecorded",
                EntityType = nameof(IncidentReport),
                EntityId = report.Id.ToString(),
                PreviousValueJson = """{"verificationStatus":"Pending"}""",
                NewValueJson = $$"""{"verificationStatus":"{{report.VerificationStatus}}"}""",
                CreatedAt = lastEvent.CreatedAt
            });
        }

        return logs;
    }
}
