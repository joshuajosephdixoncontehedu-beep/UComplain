CREATE TABLE IF NOT EXISTS "__EFMigrationsHistory" (
    "MigrationId" character varying(150) NOT NULL,
    "ProductVersion" character varying(32) NOT NULL,
    CONSTRAINT "PK___EFMigrationsHistory" PRIMARY KEY ("MigrationId")
);

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260813205346_InitialCreate') THEN
    CREATE SEQUENCE case_reference_seq START WITH 1 INCREMENT BY 1 NO CYCLE;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260813205346_InitialCreate') THEN
    CREATE TABLE admin_users (
        "Id" uuid NOT NULL,
        "FullName" character varying(200) NOT NULL,
        "Email" character varying(320) NOT NULL,
        "PasswordHash" character varying(200) NOT NULL,
        "Role" character varying(32) NOT NULL,
        "IsActive" boolean NOT NULL,
        "LastLoginAt" timestamp with time zone,
        "CreatedAt" timestamp with time zone NOT NULL,
        "UpdatedAt" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_admin_users" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260813205346_InitialCreate') THEN
    CREATE TABLE incident_categories (
        "Id" uuid NOT NULL,
        "Name" character varying(120) NOT NULL,
        "Description" character varying(500) NOT NULL,
        "DefaultPriority" character varying(16) NOT NULL,
        "SlaHours" integer NOT NULL,
        "IsActive" boolean NOT NULL,
        "DisplayOrder" integer NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        "UpdatedAt" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_incident_categories" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260813205346_InitialCreate') THEN
    CREATE TABLE reporters (
        "Id" uuid NOT NULL,
        "WhatsAppNumberHash" character varying(128) NOT NULL,
        "MaskedContactReference" character varying(64) NOT NULL,
        "VerificationStatus" character varying(32) NOT NULL,
        "ConsentAt" timestamp with time zone,
        "IsRestricted" boolean NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        "UpdatedAt" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_reporters" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260813205346_InitialCreate') THEN
    CREATE TABLE audit_logs (
        "Id" uuid NOT NULL,
        "AdminUserId" uuid,
        "Action" character varying(100) NOT NULL,
        "EntityType" character varying(100) NOT NULL,
        "EntityId" character varying(64) NOT NULL,
        "PreviousValueJson" text,
        "NewValueJson" text,
        "IpAddress" character varying(64),
        "UserAgent" character varying(500),
        "CreatedAt" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_audit_logs" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_audit_logs_admin_users_AdminUserId" FOREIGN KEY ("AdminUserId") REFERENCES admin_users ("Id") ON DELETE SET NULL
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260813205346_InitialCreate') THEN
    CREATE TABLE refresh_tokens (
        "Id" uuid NOT NULL,
        "AdminUserId" uuid NOT NULL,
        "TokenHash" character varying(200) NOT NULL,
        "ExpiresAt" timestamp with time zone NOT NULL,
        "RevokedAt" timestamp with time zone,
        "ReplacedByTokenHash" character varying(200),
        "CreatedAt" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_refresh_tokens" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_refresh_tokens_admin_users_AdminUserId" FOREIGN KEY ("AdminUserId") REFERENCES admin_users ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260813205346_InitialCreate') THEN
    CREATE TABLE incident_reports (
        "Id" uuid NOT NULL,
        "CaseReference" character varying(32) NOT NULL DEFAULT ('CIRS-' || EXTRACT(YEAR FROM now()) || '-' || LPAD(nextval('case_reference_seq')::text, 6, '0')),
        "ReporterId" uuid NOT NULL,
        "CategoryId" uuid NOT NULL,
        "SourceChannel" character varying(16) NOT NULL,
        "Description" character varying(4000) NOT NULL,
        "IncidentOccurredAt" timestamp with time zone NOT NULL,
        "LocationDescription" character varying(300) NOT NULL,
        "Latitude" double precision,
        "Longitude" double precision,
        "MediaReference" character varying(500),
        "VerificationStatus" character varying(32) NOT NULL,
        "CaseStatus" character varying(32) NOT NULL,
        "Priority" character varying(16) NOT NULL,
        "AssignedAdminId" uuid,
        "ResolutionSummary" character varying(4000),
        "CreatedAt" timestamp with time zone NOT NULL,
        "UpdatedAt" timestamp with time zone NOT NULL,
        "ClosedAt" timestamp with time zone,
        CONSTRAINT "PK_incident_reports" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_incident_reports_admin_users_AssignedAdminId" FOREIGN KEY ("AssignedAdminId") REFERENCES admin_users ("Id") ON DELETE SET NULL,
        CONSTRAINT "FK_incident_reports_incident_categories_CategoryId" FOREIGN KEY ("CategoryId") REFERENCES incident_categories ("Id") ON DELETE RESTRICT,
        CONSTRAINT "FK_incident_reports_reporters_ReporterId" FOREIGN KEY ("ReporterId") REFERENCES reporters ("Id") ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260813205346_InitialCreate') THEN
    CREATE TABLE internal_notes (
        "Id" uuid NOT NULL,
        "IncidentReportId" uuid NOT NULL,
        "Content" character varying(4000) NOT NULL,
        "CreatedByAdminId" uuid NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        "UpdatedAt" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_internal_notes" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_internal_notes_admin_users_CreatedByAdminId" FOREIGN KEY ("CreatedByAdminId") REFERENCES admin_users ("Id") ON DELETE RESTRICT,
        CONSTRAINT "FK_internal_notes_incident_reports_IncidentReportId" FOREIGN KEY ("IncidentReportId") REFERENCES incident_reports ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260813205346_InitialCreate') THEN
    CREATE TABLE report_assignments (
        "Id" uuid NOT NULL,
        "IncidentReportId" uuid NOT NULL,
        "AdminUserId" uuid NOT NULL,
        "AssignedByAdminId" uuid NOT NULL,
        "AssignedAt" timestamp with time zone NOT NULL,
        "UnassignedAt" timestamp with time zone,
        CONSTRAINT "PK_report_assignments" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_report_assignments_admin_users_AdminUserId" FOREIGN KEY ("AdminUserId") REFERENCES admin_users ("Id") ON DELETE RESTRICT,
        CONSTRAINT "FK_report_assignments_admin_users_AssignedByAdminId" FOREIGN KEY ("AssignedByAdminId") REFERENCES admin_users ("Id") ON DELETE RESTRICT,
        CONSTRAINT "FK_report_assignments_incident_reports_IncidentReportId" FOREIGN KEY ("IncidentReportId") REFERENCES incident_reports ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260813205346_InitialCreate') THEN
    CREATE TABLE status_histories (
        "Id" uuid NOT NULL,
        "IncidentReportId" uuid NOT NULL,
        "PreviousStatus" character varying(32) NOT NULL,
        "NewStatus" character varying(32) NOT NULL,
        "ChangedByAdminId" uuid NOT NULL,
        "Notes" character varying(2000),
        "CreatedAt" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_status_histories" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_status_histories_admin_users_ChangedByAdminId" FOREIGN KEY ("ChangedByAdminId") REFERENCES admin_users ("Id") ON DELETE RESTRICT,
        CONSTRAINT "FK_status_histories_incident_reports_IncidentReportId" FOREIGN KEY ("IncidentReportId") REFERENCES incident_reports ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260813205346_InitialCreate') THEN
    CREATE TABLE verification_events (
        "Id" uuid NOT NULL,
        "IncidentReportId" uuid NOT NULL,
        "ReporterId" uuid NOT NULL,
        "VerificationMethod" character varying(32) NOT NULL,
        "Result" character varying(32) NOT NULL,
        "AttemptNumber" integer NOT NULL,
        "Notes" character varying(2000),
        "PerformedByAdminId" uuid,
        "CreatedAt" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_verification_events" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_verification_events_admin_users_PerformedByAdminId" FOREIGN KEY ("PerformedByAdminId") REFERENCES admin_users ("Id") ON DELETE SET NULL,
        CONSTRAINT "FK_verification_events_incident_reports_IncidentReportId" FOREIGN KEY ("IncidentReportId") REFERENCES incident_reports ("Id") ON DELETE CASCADE,
        CONSTRAINT "FK_verification_events_reporters_ReporterId" FOREIGN KEY ("ReporterId") REFERENCES reporters ("Id") ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260813205346_InitialCreate') THEN
    CREATE UNIQUE INDEX "IX_admin_users_Email" ON admin_users ("Email");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260813205346_InitialCreate') THEN
    CREATE INDEX "IX_audit_logs_AdminUserId" ON audit_logs ("AdminUserId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260813205346_InitialCreate') THEN
    CREATE INDEX "IX_audit_logs_CreatedAt" ON audit_logs ("CreatedAt");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260813205346_InitialCreate') THEN
    CREATE INDEX "IX_audit_logs_EntityType_EntityId" ON audit_logs ("EntityType", "EntityId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260813205346_InitialCreate') THEN
    CREATE UNIQUE INDEX "IX_incident_categories_Name" ON incident_categories ("Name");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260813205346_InitialCreate') THEN
    CREATE INDEX "IX_incident_reports_AssignedAdminId" ON incident_reports ("AssignedAdminId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260813205346_InitialCreate') THEN
    CREATE UNIQUE INDEX "IX_incident_reports_CaseReference" ON incident_reports ("CaseReference");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260813205346_InitialCreate') THEN
    CREATE INDEX "IX_incident_reports_CaseStatus" ON incident_reports ("CaseStatus");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260813205346_InitialCreate') THEN
    CREATE INDEX "IX_incident_reports_CategoryId" ON incident_reports ("CategoryId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260813205346_InitialCreate') THEN
    CREATE INDEX "IX_incident_reports_CreatedAt" ON incident_reports ("CreatedAt");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260813205346_InitialCreate') THEN
    CREATE INDEX "IX_incident_reports_Priority" ON incident_reports ("Priority");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260813205346_InitialCreate') THEN
    CREATE INDEX "IX_incident_reports_ReporterId" ON incident_reports ("ReporterId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260813205346_InitialCreate') THEN
    CREATE INDEX "IX_incident_reports_VerificationStatus" ON incident_reports ("VerificationStatus");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260813205346_InitialCreate') THEN
    CREATE INDEX "IX_internal_notes_CreatedByAdminId" ON internal_notes ("CreatedByAdminId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260813205346_InitialCreate') THEN
    CREATE INDEX "IX_internal_notes_IncidentReportId" ON internal_notes ("IncidentReportId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260813205346_InitialCreate') THEN
    CREATE INDEX "IX_refresh_tokens_AdminUserId" ON refresh_tokens ("AdminUserId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260813205346_InitialCreate') THEN
    CREATE UNIQUE INDEX "IX_refresh_tokens_TokenHash" ON refresh_tokens ("TokenHash");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260813205346_InitialCreate') THEN
    CREATE INDEX "IX_report_assignments_AdminUserId" ON report_assignments ("AdminUserId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260813205346_InitialCreate') THEN
    CREATE INDEX "IX_report_assignments_AssignedByAdminId" ON report_assignments ("AssignedByAdminId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260813205346_InitialCreate') THEN
    CREATE INDEX "IX_report_assignments_IncidentReportId" ON report_assignments ("IncidentReportId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260813205346_InitialCreate') THEN
    CREATE UNIQUE INDEX "IX_reporters_WhatsAppNumberHash" ON reporters ("WhatsAppNumberHash");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260813205346_InitialCreate') THEN
    CREATE INDEX "IX_status_histories_ChangedByAdminId" ON status_histories ("ChangedByAdminId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260813205346_InitialCreate') THEN
    CREATE INDEX "IX_status_histories_IncidentReportId" ON status_histories ("IncidentReportId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260813205346_InitialCreate') THEN
    CREATE INDEX "IX_verification_events_IncidentReportId" ON verification_events ("IncidentReportId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260813205346_InitialCreate') THEN
    CREATE INDEX "IX_verification_events_PerformedByAdminId" ON verification_events ("PerformedByAdminId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260813205346_InitialCreate') THEN
    CREATE INDEX "IX_verification_events_ReporterId" ON verification_events ("ReporterId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260813205346_InitialCreate') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260813205346_InitialCreate', '9.0.19');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260813220300_AddSystemSettings') THEN
    CREATE TABLE system_settings (
        "Id" uuid NOT NULL,
        "OrganizationName" character varying(200) NOT NULL,
        "OrganizationContactEmail" character varying(320) NOT NULL,
        "NotifyOnNewVerifiedReport" boolean NOT NULL,
        "NotifyOnCriticalPriority" boolean NOT NULL,
        "DefaultVerificationSlaHours" integer NOT NULL,
        "DuplicateDetectionWindowHours" integer NOT NULL,
        "ReporterDataRetentionMonths" integer NOT NULL,
        "AuditLogRetentionMonths" integer NOT NULL,
        "WhatsAppIntegrationEnabled" boolean NOT NULL,
        "WhatsAppPlaceholderNote" character varying(1000),
        "UpdatedAt" timestamp with time zone NOT NULL,
        "UpdatedByAdminId" uuid,
        CONSTRAINT "PK_system_settings" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260813220300_AddSystemSettings') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260813220300_AddSystemSettings', '9.0.19');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260814231217_AddReporterMobileAuthAndMedia') THEN
    DROP INDEX "IX_reporters_WhatsAppNumberHash";
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260814231217_AddReporterMobileAuthAndMedia') THEN
    ALTER TABLE reporters ADD "Email" character varying(320);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260814231217_AddReporterMobileAuthAndMedia') THEN
    ALTER TABLE reporters ADD "EmailVerifiedAt" timestamp with time zone;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260814231217_AddReporterMobileAuthAndMedia') THEN
    ALTER TABLE reporters ADD "FullName" character varying(200);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260814231217_AddReporterMobileAuthAndMedia') THEN
    ALTER TABLE reporters ADD "IsActive" boolean NOT NULL DEFAULT TRUE;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260814231217_AddReporterMobileAuthAndMedia') THEN
    ALTER TABLE reporters ADD "LastLoginAt" timestamp with time zone;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260814231217_AddReporterMobileAuthAndMedia') THEN
    ALTER TABLE reporters ADD "NormalizedEmail" character varying(320);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260814231217_AddReporterMobileAuthAndMedia') THEN
    ALTER TABLE reporters ADD "PasswordHash" character varying(200);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260814231217_AddReporterMobileAuthAndMedia') THEN
    ALTER TABLE reporters ADD "PhoneNumber" character varying(32);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260814231217_AddReporterMobileAuthAndMedia') THEN
    ALTER TABLE reporters ADD "RestrictionReason" character varying(500);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260814231217_AddReporterMobileAuthAndMedia') THEN
    CREATE TABLE email_otp_verifications (
        "Id" uuid NOT NULL,
        "ReporterId" uuid,
        "Email" character varying(320) NOT NULL,
        "Purpose" character varying(32) NOT NULL,
        "CodeHash" character varying(200) NOT NULL,
        "ExpiresAt" timestamp with time zone NOT NULL,
        "AttemptCount" integer NOT NULL,
        "MaxAttempts" integer NOT NULL,
        "IsUsed" boolean NOT NULL,
        "UsedAt" timestamp with time zone,
        "CreatedAt" timestamp with time zone NOT NULL,
        "RequestIp" character varying(64),
        "UserAgent" character varying(500),
        CONSTRAINT "PK_email_otp_verifications" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_email_otp_verifications_reporters_ReporterId" FOREIGN KEY ("ReporterId") REFERENCES reporters ("Id") ON DELETE SET NULL
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260814231217_AddReporterMobileAuthAndMedia') THEN
    CREATE TABLE incident_media_attachments (
        "Id" uuid NOT NULL,
        "IncidentReportId" uuid NOT NULL,
        "FileName" character varying(255) NOT NULL,
        "StoragePath" character varying(1000) NOT NULL,
        "PublicOrSignedUrlReference" character varying(1000),
        "MediaType" character varying(16) NOT NULL,
        "MimeType" character varying(150) NOT NULL,
        "FileSizeBytes" bigint NOT NULL,
        "SortOrder" integer NOT NULL,
        "UploadedAt" timestamp with time zone NOT NULL,
        "UploadedByReporterId" uuid,
        "IsDeleted" boolean NOT NULL,
        "DeletedAt" timestamp with time zone,
        CONSTRAINT "PK_incident_media_attachments" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_incident_media_attachments_incident_reports_IncidentReportId" FOREIGN KEY ("IncidentReportId") REFERENCES incident_reports ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260814231217_AddReporterMobileAuthAndMedia') THEN
    CREATE TABLE reporter_refresh_tokens (
        "Id" uuid NOT NULL,
        "ReporterId" uuid NOT NULL,
        "TokenHash" character varying(200) NOT NULL,
        "ExpiresAt" timestamp with time zone NOT NULL,
        "RevokedAt" timestamp with time zone,
        "ReplacedByTokenHash" character varying(200),
        "CreatedAt" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_reporter_refresh_tokens" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_reporter_refresh_tokens_reporters_ReporterId" FOREIGN KEY ("ReporterId") REFERENCES reporters ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260814231217_AddReporterMobileAuthAndMedia') THEN
    CREATE UNIQUE INDEX "IX_reporters_NormalizedEmail" ON reporters ("NormalizedEmail") WHERE "NormalizedEmail" IS NOT NULL;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260814231217_AddReporterMobileAuthAndMedia') THEN
    CREATE UNIQUE INDEX "IX_reporters_WhatsAppNumberHash" ON reporters ("WhatsAppNumberHash") WHERE "WhatsAppNumberHash" <> '';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260814231217_AddReporterMobileAuthAndMedia') THEN
    CREATE INDEX "IX_email_otp_verifications_Email_Purpose_IsUsed_ExpiresAt" ON email_otp_verifications ("Email", "Purpose", "IsUsed", "ExpiresAt");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260814231217_AddReporterMobileAuthAndMedia') THEN
    CREATE INDEX "IX_email_otp_verifications_ReporterId" ON email_otp_verifications ("ReporterId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260814231217_AddReporterMobileAuthAndMedia') THEN
    CREATE INDEX "IX_incident_media_attachments_IncidentReportId_SortOrder" ON incident_media_attachments ("IncidentReportId", "SortOrder");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260814231217_AddReporterMobileAuthAndMedia') THEN
    CREATE UNIQUE INDEX "IX_incident_media_attachments_StoragePath" ON incident_media_attachments ("StoragePath");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260814231217_AddReporterMobileAuthAndMedia') THEN
    CREATE INDEX "IX_reporter_refresh_tokens_ReporterId" ON reporter_refresh_tokens ("ReporterId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260814231217_AddReporterMobileAuthAndMedia') THEN
    CREATE UNIQUE INDEX "IX_reporter_refresh_tokens_TokenHash" ON reporter_refresh_tokens ("TokenHash");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260814231217_AddReporterMobileAuthAndMedia') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260814231217_AddReporterMobileAuthAndMedia', '9.0.19');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260815093922_AddMobileWave2Reconciliation') THEN
    ALTER TABLE reporters ADD "LanguagePreference" text;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260815093922_AddMobileWave2Reconciliation') THEN
    ALTER TABLE reporters ADD "TermsAcceptedAt" timestamp with time zone;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260815093922_AddMobileWave2Reconciliation') THEN
    ALTER TABLE reporters ADD "TermsAcceptedVersion" text;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260815093922_AddMobileWave2Reconciliation') THEN
    ALTER TABLE incident_reports ADD "DuplicateOfReportId" uuid;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260815093922_AddMobileWave2Reconciliation') THEN
    ALTER TABLE incident_reports ADD "IsPubliclyVisible" boolean NOT NULL DEFAULT FALSE;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260815093922_AddMobileWave2Reconciliation') THEN
    ALTER TABLE incident_reports ADD "WithdrawalReason" character varying(1000);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260815093922_AddMobileWave2Reconciliation') THEN
    ALTER TABLE incident_reports ADD "WithdrawnAt" timestamp with time zone;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260815093922_AddMobileWave2Reconciliation') THEN
    ALTER TABLE incident_categories ADD "ColourToken" character varying(40);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260815093922_AddMobileWave2Reconciliation') THEN
    ALTER TABLE incident_categories ADD "IconKey" character varying(80);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260815093922_AddMobileWave2Reconciliation') THEN
    ALTER TABLE incident_categories ADD "Slug" character varying(80);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260815093922_AddMobileWave2Reconciliation') THEN
    CREATE TABLE reporter_privacy_settings (
        "Id" uuid NOT NULL,
        "ReporterId" uuid NOT NULL,
        "UsePreciseLocation" boolean NOT NULL,
        "ShowOnPublicMap" boolean NOT NULL,
        "AllowResponderContact" boolean NOT NULL,
        "UpdatedAt" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_reporter_privacy_settings" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_reporter_privacy_settings_reporters_ReporterId" FOREIGN KEY ("ReporterId") REFERENCES reporters ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260815093922_AddMobileWave2Reconciliation') THEN
    CREATE INDEX "IX_incident_reports_DuplicateOfReportId" ON incident_reports ("DuplicateOfReportId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260815093922_AddMobileWave2Reconciliation') THEN
    CREATE INDEX "IX_incident_reports_IsPubliclyVisible" ON incident_reports ("IsPubliclyVisible");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260815093922_AddMobileWave2Reconciliation') THEN
    CREATE UNIQUE INDEX "IX_incident_categories_Slug" ON incident_categories ("Slug") WHERE "Slug" IS NOT NULL;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260815093922_AddMobileWave2Reconciliation') THEN
    CREATE UNIQUE INDEX "IX_reporter_privacy_settings_ReporterId" ON reporter_privacy_settings ("ReporterId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260815093922_AddMobileWave2Reconciliation') THEN
    ALTER TABLE incident_reports ADD CONSTRAINT "FK_incident_reports_incident_reports_DuplicateOfReportId" FOREIGN KEY ("DuplicateOfReportId") REFERENCES incident_reports ("Id") ON DELETE RESTRICT;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260815093922_AddMobileWave2Reconciliation') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260815093922_AddMobileWave2Reconciliation', '9.0.19');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260815100945_AddReporterConsentAndSessionRemember') THEN
    ALTER TABLE reporter_refresh_tokens ADD "IsRemembered" boolean NOT NULL DEFAULT TRUE;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260815100945_AddReporterConsentAndSessionRemember') THEN
    CREATE TABLE reporter_consents (
        "Id" uuid NOT NULL,
        "ReporterId" uuid NOT NULL,
        "ConsentType" character varying(32) NOT NULL,
        "Granted" boolean NOT NULL,
        "PolicyVersion" character varying(40) NOT NULL,
        "GrantedAt" timestamp with time zone NOT NULL,
        "RevokedAt" timestamp with time zone,
        CONSTRAINT "PK_reporter_consents" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_reporter_consents_reporters_ReporterId" FOREIGN KEY ("ReporterId") REFERENCES reporters ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260815100945_AddReporterConsentAndSessionRemember') THEN
    CREATE INDEX "IX_reporter_consents_ReporterId_ConsentType_GrantedAt" ON reporter_consents ("ReporterId", "ConsentType", "GrantedAt");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260815100945_AddReporterConsentAndSessionRemember') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260815100945_AddReporterConsentAndSessionRemember', '9.0.19');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260815102839_AddReportDraftsAndCategoryCatalogue') THEN
    ALTER TABLE incident_reports ADD "Landmark" character varying(300);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260815102839_AddReportDraftsAndCategoryCatalogue') THEN
    ALTER TABLE incident_reports ADD "SubmittedAt" timestamp with time zone;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260815102839_AddReportDraftsAndCategoryCatalogue') THEN
    ALTER TABLE incident_reports ADD "TruthDeclarationAcceptedAt" timestamp with time zone;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260815102839_AddReportDraftsAndCategoryCatalogue') THEN
    CREATE TABLE report_drafts (
        "Id" uuid NOT NULL,
        "ReporterId" uuid NOT NULL,
        "CategoryId" uuid,
        "Description" character varying(4000),
        "IncidentOccurredAt" timestamp with time zone,
        "InitialPrioritySignal" character varying(16),
        "LocationDescription" character varying(300),
        "Latitude" double precision,
        "Longitude" double precision,
        "Landmark" character varying(300),
        "SubmittedReportId" uuid,
        "CreatedAt" timestamp with time zone NOT NULL,
        "UpdatedAt" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_report_drafts" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_report_drafts_incident_categories_CategoryId" FOREIGN KEY ("CategoryId") REFERENCES incident_categories ("Id") ON DELETE RESTRICT,
        CONSTRAINT "FK_report_drafts_incident_reports_SubmittedReportId" FOREIGN KEY ("SubmittedReportId") REFERENCES incident_reports ("Id") ON DELETE RESTRICT,
        CONSTRAINT "FK_report_drafts_reporters_ReporterId" FOREIGN KEY ("ReporterId") REFERENCES reporters ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260815102839_AddReportDraftsAndCategoryCatalogue') THEN
    CREATE TABLE report_draft_attachments (
        "Id" uuid NOT NULL,
        "ReportDraftId" uuid NOT NULL,
        "FileName" character varying(255) NOT NULL,
        "StoragePath" character varying(1000) NOT NULL,
        "MediaType" character varying(16) NOT NULL,
        "MimeType" character varying(150) NOT NULL,
        "FileSizeBytes" bigint NOT NULL,
        "SortOrder" integer NOT NULL,
        "UploadedAt" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_report_draft_attachments" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_report_draft_attachments_report_drafts_ReportDraftId" FOREIGN KEY ("ReportDraftId") REFERENCES report_drafts ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260815102839_AddReportDraftsAndCategoryCatalogue') THEN
    CREATE INDEX "IX_report_draft_attachments_ReportDraftId_SortOrder" ON report_draft_attachments ("ReportDraftId", "SortOrder");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260815102839_AddReportDraftsAndCategoryCatalogue') THEN
    CREATE UNIQUE INDEX "IX_report_draft_attachments_StoragePath" ON report_draft_attachments ("StoragePath");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260815102839_AddReportDraftsAndCategoryCatalogue') THEN
    CREATE INDEX "IX_report_drafts_CategoryId" ON report_drafts ("CategoryId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260815102839_AddReportDraftsAndCategoryCatalogue') THEN
    CREATE INDEX "IX_report_drafts_ReporterId" ON report_drafts ("ReporterId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260815102839_AddReportDraftsAndCategoryCatalogue') THEN
    CREATE UNIQUE INDEX "IX_report_drafts_SubmittedReportId" ON report_drafts ("SubmittedReportId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260815102839_AddReportDraftsAndCategoryCatalogue') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260815102839_AddReportDraftsAndCategoryCatalogue', '9.0.19');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260815104645_AddMobileReportsTrackingPhase4') THEN
    CREATE TABLE report_information_additions (
        "Id" uuid NOT NULL,
        "IncidentReportId" uuid NOT NULL,
        "ReporterId" uuid NOT NULL,
        "Message" character varying(2000) NOT NULL,
        "AttachmentId" uuid,
        "CreatedAt" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_report_information_additions" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_report_information_additions_incident_media_attachments_Att~" FOREIGN KEY ("AttachmentId") REFERENCES incident_media_attachments ("Id") ON DELETE SET NULL,
        CONSTRAINT "FK_report_information_additions_incident_reports_IncidentRepor~" FOREIGN KEY ("IncidentReportId") REFERENCES incident_reports ("Id") ON DELETE CASCADE,
        CONSTRAINT "FK_report_information_additions_reporters_ReporterId" FOREIGN KEY ("ReporterId") REFERENCES reporters ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260815104645_AddMobileReportsTrackingPhase4') THEN
    CREATE INDEX "IX_report_information_additions_AttachmentId" ON report_information_additions ("AttachmentId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260815104645_AddMobileReportsTrackingPhase4') THEN
    CREATE INDEX "IX_report_information_additions_IncidentReportId" ON report_information_additions ("IncidentReportId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260815104645_AddMobileReportsTrackingPhase4') THEN
    CREATE INDEX "IX_report_information_additions_ReporterId" ON report_information_additions ("ReporterId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260815104645_AddMobileReportsTrackingPhase4') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260815104645_AddMobileReportsTrackingPhase4', '9.0.19');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260815110503_AddClarificationLoopPhase5') THEN
    CREATE TABLE clarification_requests (
        "Id" uuid NOT NULL,
        "IncidentReportId" uuid NOT NULL,
        "RequestedByAdminId" uuid NOT NULL,
        "Message" character varying(2000) NOT NULL,
        "RequestedAt" timestamp with time zone NOT NULL,
        "DueAt" timestamp with time zone NOT NULL,
        "ResolvedAt" timestamp with time zone,
        "AutoClosedAt" timestamp with time zone,
        CONSTRAINT "PK_clarification_requests" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_clarification_requests_admin_users_RequestedByAdminId" FOREIGN KEY ("RequestedByAdminId") REFERENCES admin_users ("Id") ON DELETE RESTRICT,
        CONSTRAINT "FK_clarification_requests_incident_reports_IncidentReportId" FOREIGN KEY ("IncidentReportId") REFERENCES incident_reports ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260815110503_AddClarificationLoopPhase5') THEN
    CREATE TABLE clarification_responses (
        "Id" uuid NOT NULL,
        "ClarificationRequestId" uuid NOT NULL,
        "Message" character varying(2000) NOT NULL,
        "AttachmentId" uuid,
        "RespondedAt" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_clarification_responses" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_clarification_responses_clarification_requests_Clarificatio~" FOREIGN KEY ("ClarificationRequestId") REFERENCES clarification_requests ("Id") ON DELETE CASCADE,
        CONSTRAINT "FK_clarification_responses_incident_media_attachments_Attachme~" FOREIGN KEY ("AttachmentId") REFERENCES incident_media_attachments ("Id") ON DELETE SET NULL
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260815110503_AddClarificationLoopPhase5') THEN
    CREATE INDEX "IX_clarification_requests_IncidentReportId" ON clarification_requests ("IncidentReportId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260815110503_AddClarificationLoopPhase5') THEN
    CREATE INDEX "IX_clarification_requests_RequestedByAdminId" ON clarification_requests ("RequestedByAdminId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260815110503_AddClarificationLoopPhase5') THEN
    CREATE INDEX "IX_clarification_responses_AttachmentId" ON clarification_responses ("AttachmentId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260815110503_AddClarificationLoopPhase5') THEN
    CREATE INDEX "IX_clarification_responses_ClarificationRequestId" ON clarification_responses ("ClarificationRequestId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260815110503_AddClarificationLoopPhase5') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260815110503_AddClarificationLoopPhase5', '9.0.19');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260815142620_AddNotificationsPhase6') THEN
    CREATE TABLE device_tokens (
        "Id" uuid NOT NULL,
        "ReporterId" uuid NOT NULL,
        "Platform" character varying(16) NOT NULL,
        "Token" character varying(500) NOT NULL,
        "LastSeenAt" timestamp with time zone NOT NULL,
        "RevokedAt" timestamp with time zone,
        "CreatedAt" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_device_tokens" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_device_tokens_reporters_ReporterId" FOREIGN KEY ("ReporterId") REFERENCES reporters ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260815142620_AddNotificationsPhase6') THEN
    CREATE TABLE notifications (
        "Id" uuid NOT NULL,
        "ReporterId" uuid NOT NULL,
        "Type" character varying(32) NOT NULL,
        "Title" character varying(200) NOT NULL,
        "Body" character varying(1000) NOT NULL,
        "ReportId" uuid,
        "ReadAt" timestamp with time zone,
        "CreatedAt" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_notifications" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_notifications_incident_reports_ReportId" FOREIGN KEY ("ReportId") REFERENCES incident_reports ("Id") ON DELETE CASCADE,
        CONSTRAINT "FK_notifications_reporters_ReporterId" FOREIGN KEY ("ReporterId") REFERENCES reporters ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260815142620_AddNotificationsPhase6') THEN
    CREATE INDEX "IX_device_tokens_ReporterId" ON device_tokens ("ReporterId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260815142620_AddNotificationsPhase6') THEN
    CREATE UNIQUE INDEX "IX_device_tokens_Token" ON device_tokens ("Token");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260815142620_AddNotificationsPhase6') THEN
    CREATE INDEX "IX_notifications_ReporterId_CreatedAt" ON notifications ("ReporterId", "CreatedAt");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260815142620_AddNotificationsPhase6') THEN
    CREATE INDEX "IX_notifications_ReportId" ON notifications ("ReportId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260815142620_AddNotificationsPhase6') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260815142620_AddNotificationsPhase6', '9.0.19');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260815150511_AddCompliancePhase8') THEN
    ALTER TABLE reporters ADD "AnonymizedAt" timestamp with time zone;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260815150511_AddCompliancePhase8') THEN
    CREATE TABLE account_deletion_requests (
        "Id" uuid NOT NULL,
        "ReporterId" uuid NOT NULL,
        "Status" character varying(16) NOT NULL,
        "RequestedAt" timestamp with time zone NOT NULL,
        "ScheduledForAt" timestamp with time zone NOT NULL,
        "CancelledAt" timestamp with time zone,
        "CompletedAt" timestamp with time zone,
        CONSTRAINT "PK_account_deletion_requests" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_account_deletion_requests_reporters_ReporterId" FOREIGN KEY ("ReporterId") REFERENCES reporters ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260815150511_AddCompliancePhase8') THEN
    CREATE TABLE data_export_requests (
        "Id" uuid NOT NULL,
        "ReporterId" uuid NOT NULL,
        "Status" character varying(16) NOT NULL,
        "StoragePath" character varying(1000),
        "FailureReason" character varying(1000),
        "RequestedAt" timestamp with time zone NOT NULL,
        "CompletedAt" timestamp with time zone,
        CONSTRAINT "PK_data_export_requests" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_data_export_requests_reporters_ReporterId" FOREIGN KEY ("ReporterId") REFERENCES reporters ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260815150511_AddCompliancePhase8') THEN
    CREATE INDEX "IX_account_deletion_requests_ReporterId" ON account_deletion_requests ("ReporterId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260815150511_AddCompliancePhase8') THEN
    CREATE INDEX "IX_data_export_requests_ReporterId_RequestedAt" ON data_export_requests ("ReporterId", "RequestedAt");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260815150511_AddCompliancePhase8') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260815150511_AddCompliancePhase8', '9.0.19');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260816043059_AddReporterProfilePhoto') THEN
    ALTER TABLE reporters ADD "ProfilePhotoStoragePath" character varying(500);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260816043059_AddReporterProfilePhoto') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260816043059_AddReporterProfilePhoto', '9.0.19');
    END IF;
END $EF$;
COMMIT;

