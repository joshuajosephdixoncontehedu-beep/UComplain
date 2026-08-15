using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CommunityIncidentReporting.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddNotificationsPhase6 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "device_tokens",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ReporterId = table.Column<Guid>(type: "uuid", nullable: false),
                    Platform = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    Token = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    LastSeenAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    RevokedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_device_tokens", x => x.Id);
                    table.ForeignKey(
                        name: "FK_device_tokens_reporters_ReporterId",
                        column: x => x.ReporterId,
                        principalTable: "reporters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "notifications",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ReporterId = table.Column<Guid>(type: "uuid", nullable: false),
                    Type = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Body = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    ReportId = table.Column<Guid>(type: "uuid", nullable: true),
                    ReadAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_notifications", x => x.Id);
                    table.ForeignKey(
                        name: "FK_notifications_incident_reports_ReportId",
                        column: x => x.ReportId,
                        principalTable: "incident_reports",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_notifications_reporters_ReporterId",
                        column: x => x.ReporterId,
                        principalTable: "reporters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_device_tokens_ReporterId",
                table: "device_tokens",
                column: "ReporterId");

            migrationBuilder.CreateIndex(
                name: "IX_device_tokens_Token",
                table: "device_tokens",
                column: "Token",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_notifications_ReporterId_CreatedAt",
                table: "notifications",
                columns: new[] { "ReporterId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_notifications_ReportId",
                table: "notifications",
                column: "ReportId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "device_tokens");

            migrationBuilder.DropTable(
                name: "notifications");
        }
    }
}
