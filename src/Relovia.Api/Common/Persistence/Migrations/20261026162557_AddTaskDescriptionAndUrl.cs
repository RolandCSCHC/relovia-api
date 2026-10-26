using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Relovia.Api.Common.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddTaskDescriptionAndUrl : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "description",
                table: "tasks",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "url",
                table: "tasks",
                type: "character varying(2048)",
                maxLength: 2048,
                nullable: true);

            migrationBuilder.AddCheckConstraint(
                name: "ck_tasks_description",
                table: "tasks",
                sql: "description IS NULL OR (char_length(btrim(description)) > 0 AND char_length(description) <= 1000)");

            migrationBuilder.AddCheckConstraint(
                name: "ck_tasks_url",
                table: "tasks",
                sql: "url IS NULL OR (char_length(btrim(url)) > 0 AND char_length(url) <= 2048)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_tasks_description",
                table: "tasks");

            migrationBuilder.DropCheckConstraint(
                name: "ck_tasks_url",
                table: "tasks");

            migrationBuilder.DropColumn(
                name: "description",
                table: "tasks");

            migrationBuilder.DropColumn(
                name: "url",
                table: "tasks");
        }
    }
}
