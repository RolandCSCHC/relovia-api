using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Relovia.Api.Common.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCalendarEvents : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "calendar_events",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    relocation_id = table.Column<Guid>(type: "uuid", nullable: false),
                    title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    kind = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    occurs_on = table.Column<DateOnly>(type: "date", nullable: true),
                    starts_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    ends_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    location = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_calendar_events", x => x.id);
                    table.CheckConstraint("ck_calendar_events_kind", "(kind = 'Milestone' AND occurs_on IS NOT NULL AND starts_at IS NULL AND ends_at IS NULL)\nOR (kind = 'Appointment' AND occurs_on IS NULL AND starts_at IS NOT NULL AND (ends_at IS NULL OR ends_at > starts_at))");
                    table.CheckConstraint("ck_calendar_events_location", "location IS NULL OR (char_length(btrim(location)) > 0 AND char_length(location) <= 200)");
                    table.CheckConstraint("ck_calendar_events_title", "char_length(btrim(title)) > 0 AND char_length(title) <= 200");
                    table.ForeignKey(
                        name: "fk_calendar_events_relocations_relocation_id",
                        column: x => x.relocation_id,
                        principalTable: "relocations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_calendar_events_relocation_id",
                table: "calendar_events",
                column: "relocation_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "calendar_events");
        }
    }
}
