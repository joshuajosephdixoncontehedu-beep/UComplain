using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CommunityIncidentReporting.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddReportDraftsAndCategoryCatalogue : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Landmark",
                table: "incident_reports",
                type: "character varying(300)",
                maxLength: 300,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "SubmittedAt",
                table: "incident_reports",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "TruthDeclarationAcceptedAt",
                table: "incident_reports",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "report_drafts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ReporterId = table.Column<Guid>(type: "uuid", nullable: false),
                    CategoryId = table.Column<Guid>(type: "uuid", nullable: true),
                    Description = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    IncidentOccurredAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    InitialPrioritySignal = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: true),
                    LocationDescription = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    Latitude = table.Column<double>(type: "double precision", nullable: true),
                    Longitude = table.Column<double>(type: "double precision", nullable: true),
                    Landmark = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    SubmittedReportId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_report_drafts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_report_drafts_incident_categories_CategoryId",
                        column: x => x.CategoryId,
                        principalTable: "incident_categories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_report_drafts_incident_reports_SubmittedReportId",
                        column: x => x.SubmittedReportId,
                        principalTable: "incident_reports",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_report_drafts_reporters_ReporterId",
                        column: x => x.ReporterId,
                        principalTable: "reporters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "report_draft_attachments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ReportDraftId = table.Column<Guid>(type: "uuid", nullable: false),
                    FileName = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    StoragePath = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    MediaType = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    MimeType = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    FileSizeBytes = table.Column<long>(type: "bigint", nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    UploadedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_report_draft_attachments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_report_draft_attachments_report_drafts_ReportDraftId",
                        column: x => x.ReportDraftId,
                        principalTable: "report_drafts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_report_draft_attachments_ReportDraftId_SortOrder",
                table: "report_draft_attachments",
                columns: new[] { "ReportDraftId", "SortOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_report_draft_attachments_StoragePath",
                table: "report_draft_attachments",
                column: "StoragePath",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_report_drafts_CategoryId",
                table: "report_drafts",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_report_drafts_ReporterId",
                table: "report_drafts",
                column: "ReporterId");

            migrationBuilder.CreateIndex(
                name: "IX_report_drafts_SubmittedReportId",
                table: "report_drafts",
                column: "SubmittedReportId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "report_draft_attachments");

            migrationBuilder.DropTable(
                name: "report_drafts");

            migrationBuilder.DropColumn(
                name: "Landmark",
                table: "incident_reports");

            migrationBuilder.DropColumn(
                name: "SubmittedAt",
                table: "incident_reports");

            migrationBuilder.DropColumn(
                name: "TruthDeclarationAcceptedAt",
                table: "incident_reports");
        }
    }
}
