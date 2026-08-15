using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CommunityIncidentReporting.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddReporterMobileAuthAndMedia : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_reporters_WhatsAppNumberHash",
                table: "reporters");

            migrationBuilder.AddColumn<string>(
                name: "Email",
                table: "reporters",
                type: "character varying(320)",
                maxLength: 320,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "EmailVerifiedAt",
                table: "reporters",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FullName",
                table: "reporters",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "reporters",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "LastLoginAt",
                table: "reporters",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NormalizedEmail",
                table: "reporters",
                type: "character varying(320)",
                maxLength: 320,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PasswordHash",
                table: "reporters",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PhoneNumber",
                table: "reporters",
                type: "character varying(32)",
                maxLength: 32,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RestrictionReason",
                table: "reporters",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "email_otp_verifications",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ReporterId = table.Column<Guid>(type: "uuid", nullable: true),
                    Email = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: false),
                    Purpose = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    CodeHash = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    ExpiresAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    AttemptCount = table.Column<int>(type: "integer", nullable: false),
                    MaxAttempts = table.Column<int>(type: "integer", nullable: false),
                    IsUsed = table.Column<bool>(type: "boolean", nullable: false),
                    UsedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    RequestIp = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    UserAgent = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_email_otp_verifications", x => x.Id);
                    table.ForeignKey(
                        name: "FK_email_otp_verifications_reporters_ReporterId",
                        column: x => x.ReporterId,
                        principalTable: "reporters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "incident_media_attachments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    IncidentReportId = table.Column<Guid>(type: "uuid", nullable: false),
                    FileName = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    StoragePath = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    PublicOrSignedUrlReference = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    MediaType = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    MimeType = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    FileSizeBytes = table.Column<long>(type: "bigint", nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    UploadedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UploadedByReporterId = table.Column<Guid>(type: "uuid", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_incident_media_attachments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_incident_media_attachments_incident_reports_IncidentReportId",
                        column: x => x.IncidentReportId,
                        principalTable: "incident_reports",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "reporter_refresh_tokens",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ReporterId = table.Column<Guid>(type: "uuid", nullable: false),
                    TokenHash = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    ExpiresAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    RevokedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ReplacedByTokenHash = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_reporter_refresh_tokens", x => x.Id);
                    table.ForeignKey(
                        name: "FK_reporter_refresh_tokens_reporters_ReporterId",
                        column: x => x.ReporterId,
                        principalTable: "reporters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_reporters_NormalizedEmail",
                table: "reporters",
                column: "NormalizedEmail",
                unique: true,
                filter: "\"NormalizedEmail\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_reporters_WhatsAppNumberHash",
                table: "reporters",
                column: "WhatsAppNumberHash",
                unique: true,
                filter: "\"WhatsAppNumberHash\" <> ''");

            migrationBuilder.CreateIndex(
                name: "IX_email_otp_verifications_Email_Purpose_IsUsed_ExpiresAt",
                table: "email_otp_verifications",
                columns: new[] { "Email", "Purpose", "IsUsed", "ExpiresAt" });

            migrationBuilder.CreateIndex(
                name: "IX_email_otp_verifications_ReporterId",
                table: "email_otp_verifications",
                column: "ReporterId");

            migrationBuilder.CreateIndex(
                name: "IX_incident_media_attachments_IncidentReportId_SortOrder",
                table: "incident_media_attachments",
                columns: new[] { "IncidentReportId", "SortOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_incident_media_attachments_StoragePath",
                table: "incident_media_attachments",
                column: "StoragePath",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_reporter_refresh_tokens_ReporterId",
                table: "reporter_refresh_tokens",
                column: "ReporterId");

            migrationBuilder.CreateIndex(
                name: "IX_reporter_refresh_tokens_TokenHash",
                table: "reporter_refresh_tokens",
                column: "TokenHash",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "email_otp_verifications");

            migrationBuilder.DropTable(
                name: "incident_media_attachments");

            migrationBuilder.DropTable(
                name: "reporter_refresh_tokens");

            migrationBuilder.DropIndex(
                name: "IX_reporters_NormalizedEmail",
                table: "reporters");

            migrationBuilder.DropIndex(
                name: "IX_reporters_WhatsAppNumberHash",
                table: "reporters");

            migrationBuilder.DropColumn(
                name: "Email",
                table: "reporters");

            migrationBuilder.DropColumn(
                name: "EmailVerifiedAt",
                table: "reporters");

            migrationBuilder.DropColumn(
                name: "FullName",
                table: "reporters");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "reporters");

            migrationBuilder.DropColumn(
                name: "LastLoginAt",
                table: "reporters");

            migrationBuilder.DropColumn(
                name: "NormalizedEmail",
                table: "reporters");

            migrationBuilder.DropColumn(
                name: "PasswordHash",
                table: "reporters");

            migrationBuilder.DropColumn(
                name: "PhoneNumber",
                table: "reporters");

            migrationBuilder.DropColumn(
                name: "RestrictionReason",
                table: "reporters");

            migrationBuilder.CreateIndex(
                name: "IX_reporters_WhatsAppNumberHash",
                table: "reporters",
                column: "WhatsAppNumberHash",
                unique: true);
        }
    }
}
