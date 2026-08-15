using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CommunityIncidentReporting.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddClarificationLoopPhase5 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "clarification_requests",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    IncidentReportId = table.Column<Guid>(type: "uuid", nullable: false),
                    RequestedByAdminId = table.Column<Guid>(type: "uuid", nullable: false),
                    Message = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    RequestedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    DueAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ResolvedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    AutoClosedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_clarification_requests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_clarification_requests_admin_users_RequestedByAdminId",
                        column: x => x.RequestedByAdminId,
                        principalTable: "admin_users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_clarification_requests_incident_reports_IncidentReportId",
                        column: x => x.IncidentReportId,
                        principalTable: "incident_reports",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "clarification_responses",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ClarificationRequestId = table.Column<Guid>(type: "uuid", nullable: false),
                    Message = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    AttachmentId = table.Column<Guid>(type: "uuid", nullable: true),
                    RespondedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_clarification_responses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_clarification_responses_clarification_requests_Clarificatio~",
                        column: x => x.ClarificationRequestId,
                        principalTable: "clarification_requests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_clarification_responses_incident_media_attachments_Attachme~",
                        column: x => x.AttachmentId,
                        principalTable: "incident_media_attachments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_clarification_requests_IncidentReportId",
                table: "clarification_requests",
                column: "IncidentReportId");

            migrationBuilder.CreateIndex(
                name: "IX_clarification_requests_RequestedByAdminId",
                table: "clarification_requests",
                column: "RequestedByAdminId");

            migrationBuilder.CreateIndex(
                name: "IX_clarification_responses_AttachmentId",
                table: "clarification_responses",
                column: "AttachmentId");

            migrationBuilder.CreateIndex(
                name: "IX_clarification_responses_ClarificationRequestId",
                table: "clarification_responses",
                column: "ClarificationRequestId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "clarification_responses");

            migrationBuilder.DropTable(
                name: "clarification_requests");
        }
    }
}
