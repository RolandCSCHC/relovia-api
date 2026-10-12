using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Relovia.Api.Common.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddRelocationTasks : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "tasks",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    relocation_id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    days_before_move = table.Column<int>(type: "integer", nullable: false),
                    due_date = table.Column<DateOnly>(type: "date", nullable: false),
                    phase = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    priority = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false, defaultValue: "Todo"),
                    source_template_task_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_tasks", x => x.id);
                    table.UniqueConstraint("ak_tasks_relocation_id_id", x => new { x.relocation_id, x.id });
                    table.CheckConstraint("ck_tasks_phase", "phase IN ('Preparation', 'Housing', 'Administration', 'Finances', 'Moving', 'Settlement')");
                    table.CheckConstraint("ck_tasks_priority", "priority IN ('Low', 'Normal', 'High')");
                    table.CheckConstraint("ck_tasks_status", "status IN ('Todo', 'InProgress', 'Done', 'Skipped')");
                    table.ForeignKey(
                        name: "fk_tasks_relocations_relocation_id",
                        column: x => x.relocation_id,
                        principalTable: "relocations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_tasks_task_templates_source_template_task_id",
                        column: x => x.source_template_task_id,
                        principalTable: "task_templates",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "task_dependencies",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    relocation_id = table.Column<Guid>(type: "uuid", nullable: false),
                    task_id = table.Column<Guid>(type: "uuid", nullable: false),
                    depends_on_task_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_task_dependencies", x => x.id);
                    table.ForeignKey(
                        name: "fk_task_dependencies_tasks_relocation_id_depends_on_task_id",
                        columns: x => new { x.relocation_id, x.depends_on_task_id },
                        principalTable: "tasks",
                        principalColumns: new[] { "relocation_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_task_dependencies_tasks_relocation_id_task_id",
                        columns: x => new { x.relocation_id, x.task_id },
                        principalTable: "tasks",
                        principalColumns: new[] { "relocation_id", "id" },
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_task_dependencies_relocation_id_depends_on_task_id",
                table: "task_dependencies",
                columns: new[] { "relocation_id", "depends_on_task_id" });

            migrationBuilder.CreateIndex(
                name: "ix_task_dependencies_relocation_id_task_id",
                table: "task_dependencies",
                columns: new[] { "relocation_id", "task_id" });

            migrationBuilder.CreateIndex(
                name: "ux_task_dependencies_task_id_depends_on_task_id",
                table: "task_dependencies",
                columns: new[] { "task_id", "depends_on_task_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_tasks_source_template_task_id",
                table: "tasks",
                column: "source_template_task_id");

            migrationBuilder.CreateIndex(
                name: "ux_tasks_relocation_id_code",
                table: "tasks",
                columns: new[] { "relocation_id", "code" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "task_dependencies");

            migrationBuilder.DropTable(
                name: "tasks");
        }
    }
}
