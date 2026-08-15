using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CommunityIncidentReporting.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddReporterConsentAndSessionRemember : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsRemembered",
                table: "reporter_refresh_tokens",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.CreateTable(
                name: "reporter_consents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ReporterId = table.Column<Guid>(type: "uuid", nullable: false),
                    ConsentType = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Granted = table.Column<bool>(type: "boolean", nullable: false),
                    PolicyVersion = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    GrantedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    RevokedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_reporter_consents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_reporter_consents_reporters_ReporterId",
                        column: x => x.ReporterId,
                        principalTable: "reporters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_reporter_consents_ReporterId_ConsentType_GrantedAt",
                table: "reporter_consents",
                columns: new[] { "ReporterId", "ConsentType", "GrantedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "reporter_consents");

            migrationBuilder.DropColumn(
                name: "IsRemembered",
                table: "reporter_refresh_tokens");
        }
    }
}
