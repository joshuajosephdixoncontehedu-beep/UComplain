using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CommunityIncidentReporting.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddSystemSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "system_settings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    OrganizationContactEmail = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: false),
                    NotifyOnNewVerifiedReport = table.Column<bool>(type: "boolean", nullable: false),
                    NotifyOnCriticalPriority = table.Column<bool>(type: "boolean", nullable: false),
                    DefaultVerificationSlaHours = table.Column<int>(type: "integer", nullable: false),
                    DuplicateDetectionWindowHours = table.Column<int>(type: "integer", nullable: false),
                    ReporterDataRetentionMonths = table.Column<int>(type: "integer", nullable: false),
                    AuditLogRetentionMonths = table.Column<int>(type: "integer", nullable: false),
                    WhatsAppIntegrationEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    WhatsAppPlaceholderNote = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedByAdminId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_system_settings", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "system_settings");
        }
    }
}
