using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CommunityIncidentReporting.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddMobileReportsTrackingPhase4 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "report_information_additions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    IncidentReportId = table.Column<Guid>(type: "uuid", nullable: false),
                    ReporterId = table.Column<Guid>(type: "uuid", nullable: false),
                    Message = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    AttachmentId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_report_information_additions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_report_information_additions_incident_media_attachments_Att~",
                        column: x => x.AttachmentId,
                        principalTable: "incident_media_attachments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_report_information_additions_incident_reports_IncidentRepor~",
                        column: x => x.IncidentReportId,
                        principalTable: "incident_reports",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_report_information_additions_reporters_ReporterId",
                        column: x => x.ReporterId,
                        principalTable: "reporters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_report_information_additions_AttachmentId",
                table: "report_information_additions",
                column: "AttachmentId");

            migrationBuilder.CreateIndex(
                name: "IX_report_information_additions_IncidentReportId",
                table: "report_information_additions",
                column: "IncidentReportId");

            migrationBuilder.CreateIndex(
                name: "IX_report_information_additions_ReporterId",
                table: "report_information_additions",
                column: "ReporterId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "report_information_additions");
        }
    }
}
