using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CommunityIncidentReporting.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddMobileWave2Reconciliation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "LanguagePreference",
                table: "reporters",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "TermsAcceptedAt",
                table: "reporters",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TermsAcceptedVersion",
                table: "reporters",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "DuplicateOfReportId",
                table: "incident_reports",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsPubliclyVisible",
                table: "incident_reports",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "WithdrawalReason",
                table: "incident_reports",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "WithdrawnAt",
                table: "incident_reports",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ColourToken",
                table: "incident_categories",
                type: "character varying(40)",
                maxLength: 40,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "IconKey",
                table: "incident_categories",
                type: "character varying(80)",
                maxLength: 80,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Slug",
                table: "incident_categories",
                type: "character varying(80)",
                maxLength: 80,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "reporter_privacy_settings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ReporterId = table.Column<Guid>(type: "uuid", nullable: false),
                    UsePreciseLocation = table.Column<bool>(type: "boolean", nullable: false),
                    ShowOnPublicMap = table.Column<bool>(type: "boolean", nullable: false),
                    AllowResponderContact = table.Column<bool>(type: "boolean", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_reporter_privacy_settings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_reporter_privacy_settings_reporters_ReporterId",
                        column: x => x.ReporterId,
                        principalTable: "reporters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_incident_reports_DuplicateOfReportId",
                table: "incident_reports",
                column: "DuplicateOfReportId");

            migrationBuilder.CreateIndex(
                name: "IX_incident_reports_IsPubliclyVisible",
                table: "incident_reports",
                column: "IsPubliclyVisible");

            migrationBuilder.CreateIndex(
                name: "IX_incident_categories_Slug",
                table: "incident_categories",
                column: "Slug",
                unique: true,
                filter: "\"Slug\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_reporter_privacy_settings_ReporterId",
                table: "reporter_privacy_settings",
                column: "ReporterId",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_incident_reports_incident_reports_DuplicateOfReportId",
                table: "incident_reports",
                column: "DuplicateOfReportId",
                principalTable: "incident_reports",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_incident_reports_incident_reports_DuplicateOfReportId",
                table: "incident_reports");

            migrationBuilder.DropTable(
                name: "reporter_privacy_settings");

            migrationBuilder.DropIndex(
                name: "IX_incident_reports_DuplicateOfReportId",
                table: "incident_reports");

            migrationBuilder.DropIndex(
                name: "IX_incident_reports_IsPubliclyVisible",
                table: "incident_reports");

            migrationBuilder.DropIndex(
                name: "IX_incident_categories_Slug",
                table: "incident_categories");

            migrationBuilder.DropColumn(
                name: "LanguagePreference",
                table: "reporters");

            migrationBuilder.DropColumn(
                name: "TermsAcceptedAt",
                table: "reporters");

            migrationBuilder.DropColumn(
                name: "TermsAcceptedVersion",
                table: "reporters");

            migrationBuilder.DropColumn(
                name: "DuplicateOfReportId",
                table: "incident_reports");

            migrationBuilder.DropColumn(
                name: "IsPubliclyVisible",
                table: "incident_reports");

            migrationBuilder.DropColumn(
                name: "WithdrawalReason",
                table: "incident_reports");

            migrationBuilder.DropColumn(
                name: "WithdrawnAt",
                table: "incident_reports");

            migrationBuilder.DropColumn(
                name: "ColourToken",
                table: "incident_categories");

            migrationBuilder.DropColumn(
                name: "IconKey",
                table: "incident_categories");

            migrationBuilder.DropColumn(
                name: "Slug",
                table: "incident_categories");
        }
    }
}
