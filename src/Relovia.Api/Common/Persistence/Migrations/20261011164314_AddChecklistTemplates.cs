using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Relovia.Api.Common.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddChecklistTemplates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "checklist_templates",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    version = table.Column<int>(type: "integer", nullable: false),
                    title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    scope = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    destination_country_code = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: true),
                    purpose = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    published_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_checklist_templates", x => x.id);
                    table.CheckConstraint("ck_checklist_templates_destination_country_code", "destination_country_code IS NULL OR destination_country_code ~ '^[A-Z]{2}$'");
                    table.CheckConstraint("ck_checklist_templates_purpose", "purpose IS NULL OR purpose IN ('Work', 'Study', 'Family', 'Lifestyle', 'Other')");
                    table.CheckConstraint("ck_checklist_templates_scope", "scope IN ('Domestic', 'International', 'Both')");
                    table.CheckConstraint("ck_checklist_templates_version", "version >= 1");
                });

            migrationBuilder.CreateTable(
                name: "task_templates",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    checklist_template_id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    days_before_move = table.Column<int>(type: "integer", nullable: false),
                    phase = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    priority = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_task_templates", x => x.id);
                    table.CheckConstraint("ck_task_templates_phase", "phase IN ('Preparation', 'Housing', 'Administration', 'Finances', 'Moving', 'Settlement')");
                    table.CheckConstraint("ck_task_templates_priority", "priority IN ('Low', 'Normal', 'High')");
                    table.ForeignKey(
                        name: "fk_task_templates_checklist_templates_checklist_template_id",
                        column: x => x.checklist_template_id,
                        principalTable: "checklist_templates",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "task_template_dependencies",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    task_template_id = table.Column<Guid>(type: "uuid", nullable: false),
                    depends_on_code = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_task_template_dependencies", x => x.id);
                    table.ForeignKey(
                        name: "fk_task_template_dependencies_task_templates_task_template_id",
                        column: x => x.task_template_id,
                        principalTable: "task_templates",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ux_checklist_templates_code_version",
                table: "checklist_templates",
                columns: new[] { "code", "version" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_task_template_dependencies_task_id_code",
                table: "task_template_dependencies",
                columns: new[] { "task_template_id", "depends_on_code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_task_templates_template_id_code",
                table: "task_templates",
                columns: new[] { "checklist_template_id", "code" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "task_template_dependencies");

            migrationBuilder.DropTable(
                name: "task_templates");

            migrationBuilder.DropTable(
                name: "checklist_templates");
        }
    }
}
