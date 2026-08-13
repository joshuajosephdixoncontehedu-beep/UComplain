using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CommunityIncidentReporting.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateSequence(
                name: "case_reference_seq");

            migrationBuilder.CreateTable(
                name: "admin_users",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    FullName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Email = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: false),
                    PasswordHash = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Role = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    LastLoginAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_admin_users", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "incident_categories",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    DefaultPriority = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    SlaHours = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    DisplayOrder = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_incident_categories", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "reporters",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    WhatsAppNumberHash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    MaskedContactReference = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    VerificationStatus = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    ConsentAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    IsRestricted = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_reporters", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "audit_logs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AdminUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    Action = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    EntityType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    EntityId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    PreviousValueJson = table.Column<string>(type: "text", nullable: true),
                    NewValueJson = table.Column<string>(type: "text", nullable: true),
                    IpAddress = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    UserAgent = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_audit_logs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_audit_logs_admin_users_AdminUserId",
                        column: x => x.AdminUserId,
                        principalTable: "admin_users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "refresh_tokens",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AdminUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    TokenHash = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    ExpiresAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    RevokedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ReplacedByTokenHash = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_refresh_tokens", x => x.Id);
                    table.ForeignKey(
                        name: "FK_refresh_tokens_admin_users_AdminUserId",
                        column: x => x.AdminUserId,
                        principalTable: "admin_users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "incident_reports",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CaseReference = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false, defaultValueSql: "'CIRS-' || EXTRACT(YEAR FROM now()) || '-' || LPAD(nextval('case_reference_seq')::text, 6, '0')"),
                    ReporterId = table.Column<Guid>(type: "uuid", nullable: false),
                    CategoryId = table.Column<Guid>(type: "uuid", nullable: false),
                    SourceChannel = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    Description = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                    IncidentOccurredAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    LocationDescription = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    Latitude = table.Column<double>(type: "double precision", nullable: true),
                    Longitude = table.Column<double>(type: "double precision", nullable: true),
                    MediaReference = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    VerificationStatus = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    CaseStatus = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Priority = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    AssignedAdminId = table.Column<Guid>(type: "uuid", nullable: true),
                    ResolutionSummary = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ClosedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_incident_reports", x => x.Id);
                    table.ForeignKey(
                        name: "FK_incident_reports_admin_users_AssignedAdminId",
                        column: x => x.AssignedAdminId,
                        principalTable: "admin_users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_incident_reports_incident_categories_CategoryId",
                        column: x => x.CategoryId,
                        principalTable: "incident_categories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_incident_reports_reporters_ReporterId",
                        column: x => x.ReporterId,
                        principalTable: "reporters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "internal_notes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    IncidentReportId = table.Column<Guid>(type: "uuid", nullable: false),
                    Content = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                    CreatedByAdminId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_internal_notes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_internal_notes_admin_users_CreatedByAdminId",
                        column: x => x.CreatedByAdminId,
                        principalTable: "admin_users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_internal_notes_incident_reports_IncidentReportId",
                        column: x => x.IncidentReportId,
                        principalTable: "incident_reports",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "report_assignments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    IncidentReportId = table.Column<Guid>(type: "uuid", nullable: false),
                    AdminUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    AssignedByAdminId = table.Column<Guid>(type: "uuid", nullable: false),
                    AssignedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UnassignedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_report_assignments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_report_assignments_admin_users_AdminUserId",
                        column: x => x.AdminUserId,
                        principalTable: "admin_users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_report_assignments_admin_users_AssignedByAdminId",
                        column: x => x.AssignedByAdminId,
                        principalTable: "admin_users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_report_assignments_incident_reports_IncidentReportId",
                        column: x => x.IncidentReportId,
                        principalTable: "incident_reports",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "status_histories",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    IncidentReportId = table.Column<Guid>(type: "uuid", nullable: false),
                    PreviousStatus = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    NewStatus = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    ChangedByAdminId = table.Column<Guid>(type: "uuid", nullable: false),
                    Notes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_status_histories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_status_histories_admin_users_ChangedByAdminId",
                        column: x => x.ChangedByAdminId,
                        principalTable: "admin_users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_status_histories_incident_reports_IncidentReportId",
                        column: x => x.IncidentReportId,
                        principalTable: "incident_reports",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "verification_events",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    IncidentReportId = table.Column<Guid>(type: "uuid", nullable: false),
                    ReporterId = table.Column<Guid>(type: "uuid", nullable: false),
                    VerificationMethod = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Result = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    AttemptNumber = table.Column<int>(type: "integer", nullable: false),
                    Notes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    PerformedByAdminId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_verification_events", x => x.Id);
                    table.ForeignKey(
                        name: "FK_verification_events_admin_users_PerformedByAdminId",
                        column: x => x.PerformedByAdminId,
                        principalTable: "admin_users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_verification_events_incident_reports_IncidentReportId",
                        column: x => x.IncidentReportId,
                        principalTable: "incident_reports",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_verification_events_reporters_ReporterId",
                        column: x => x.ReporterId,
                        principalTable: "reporters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_admin_users_Email",
                table: "admin_users",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_audit_logs_AdminUserId",
                table: "audit_logs",
                column: "AdminUserId");

            migrationBuilder.CreateIndex(
                name: "IX_audit_logs_CreatedAt",
                table: "audit_logs",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_audit_logs_EntityType_EntityId",
                table: "audit_logs",
                columns: new[] { "EntityType", "EntityId" });

            migrationBuilder.CreateIndex(
                name: "IX_incident_categories_Name",
                table: "incident_categories",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_incident_reports_AssignedAdminId",
                table: "incident_reports",
                column: "AssignedAdminId");

            migrationBuilder.CreateIndex(
                name: "IX_incident_reports_CaseReference",
                table: "incident_reports",
                column: "CaseReference",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_incident_reports_CaseStatus",
                table: "incident_reports",
                column: "CaseStatus");

            migrationBuilder.CreateIndex(
                name: "IX_incident_reports_CategoryId",
                table: "incident_reports",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_incident_reports_CreatedAt",
                table: "incident_reports",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_incident_reports_Priority",
                table: "incident_reports",
                column: "Priority");

            migrationBuilder.CreateIndex(
                name: "IX_incident_reports_ReporterId",
                table: "incident_reports",
                column: "ReporterId");

            migrationBuilder.CreateIndex(
                name: "IX_incident_reports_VerificationStatus",
                table: "incident_reports",
                column: "VerificationStatus");

            migrationBuilder.CreateIndex(
                name: "IX_internal_notes_CreatedByAdminId",
                table: "internal_notes",
                column: "CreatedByAdminId");

            migrationBuilder.CreateIndex(
                name: "IX_internal_notes_IncidentReportId",
                table: "internal_notes",
                column: "IncidentReportId");

            migrationBuilder.CreateIndex(
                name: "IX_refresh_tokens_AdminUserId",
                table: "refresh_tokens",
                column: "AdminUserId");

            migrationBuilder.CreateIndex(
                name: "IX_refresh_tokens_TokenHash",
                table: "refresh_tokens",
                column: "TokenHash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_report_assignments_AdminUserId",
                table: "report_assignments",
                column: "AdminUserId");

            migrationBuilder.CreateIndex(
                name: "IX_report_assignments_AssignedByAdminId",
                table: "report_assignments",
                column: "AssignedByAdminId");

            migrationBuilder.CreateIndex(
                name: "IX_report_assignments_IncidentReportId",
                table: "report_assignments",
                column: "IncidentReportId");

            migrationBuilder.CreateIndex(
                name: "IX_reporters_WhatsAppNumberHash",
                table: "reporters",
                column: "WhatsAppNumberHash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_status_histories_ChangedByAdminId",
                table: "status_histories",
                column: "ChangedByAdminId");

            migrationBuilder.CreateIndex(
                name: "IX_status_histories_IncidentReportId",
                table: "status_histories",
                column: "IncidentReportId");

            migrationBuilder.CreateIndex(
                name: "IX_verification_events_IncidentReportId",
                table: "verification_events",
                column: "IncidentReportId");

            migrationBuilder.CreateIndex(
                name: "IX_verification_events_PerformedByAdminId",
                table: "verification_events",
                column: "PerformedByAdminId");

            migrationBuilder.CreateIndex(
                name: "IX_verification_events_ReporterId",
                table: "verification_events",
                column: "ReporterId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "audit_logs");

            migrationBuilder.DropTable(
                name: "internal_notes");

            migrationBuilder.DropTable(
                name: "refresh_tokens");

            migrationBuilder.DropTable(
                name: "report_assignments");

            migrationBuilder.DropTable(
                name: "status_histories");

            migrationBuilder.DropTable(
                name: "verification_events");

            migrationBuilder.DropTable(
                name: "incident_reports");

            migrationBuilder.DropTable(
                name: "admin_users");

            migrationBuilder.DropTable(
                name: "incident_categories");

            migrationBuilder.DropTable(
                name: "reporters");

            migrationBuilder.DropSequence(
                name: "case_reference_seq");
        }
    }
}
