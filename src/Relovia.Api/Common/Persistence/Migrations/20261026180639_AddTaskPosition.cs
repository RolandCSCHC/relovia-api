using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Relovia.Api.Common.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddTaskPosition : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "position",
                table: "tasks",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            // Existing lists were ordered by due date, with an empty due date last, then task code.
            migrationBuilder.Sql(
                """
                WITH ranked AS (
                    SELECT id,
                           ROW_NUMBER() OVER (
                               PARTITION BY relocation_id
                               ORDER BY due_date ASC NULLS LAST, code ASC
                           ) - 1 AS position
                    FROM tasks
                )
                UPDATE tasks AS task
                SET position = ranked.position
                FROM ranked
                WHERE task.id = ranked.id;
                """);

            migrationBuilder.Sql("ALTER TABLE tasks ALTER COLUMN position DROP DEFAULT;");

            migrationBuilder.CreateIndex(
                name: "ux_tasks_relocation_id_position",
                table: "tasks",
                columns: new[] { "relocation_id", "position" },
                unique: true);

            migrationBuilder.AddCheckConstraint(
                name: "ck_tasks_position",
                table: "tasks",
                sql: "position >= 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ux_tasks_relocation_id_position",
                table: "tasks");

            migrationBuilder.DropCheckConstraint(
                name: "ck_tasks_position",
                table: "tasks");

            migrationBuilder.DropColumn(
                name: "position",
                table: "tasks");
        }
    }
}
