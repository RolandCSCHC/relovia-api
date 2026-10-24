using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Relovia.Api.Common.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddDocumentDetails : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "category",
                table: "documents",
                type: "character varying(32)",
                maxLength: 32,
                nullable: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "expires_on",
                table: "documents",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "updated_at",
                table: "documents",
                type: "timestamptz",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.Sql("UPDATE documents SET updated_at = created_at;");

            migrationBuilder.Sql("ALTER TABLE documents ALTER COLUMN updated_at DROP DEFAULT;");

            migrationBuilder.AddUniqueConstraint(
                name: "ak_documents_relocation_id_id",
                table: "documents",
                columns: new[] { "relocation_id", "id" });

            migrationBuilder.CreateTable(
                name: "document_task_links",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    relocation_id = table.Column<Guid>(type: "uuid", nullable: false),
                    document_id = table.Column<Guid>(type: "uuid", nullable: false),
                    task_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_document_task_links", x => x.id);
                    table.ForeignKey(
                        name: "fk_document_task_links_documents_relocation_id_document_id",
                        columns: x => new { x.relocation_id, x.document_id },
                        principalTable: "documents",
                        principalColumns: new[] { "relocation_id", "id" },
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_document_task_links_relocation_tasks_relocation_id_task_id",
                        columns: x => new { x.relocation_id, x.task_id },
                        principalTable: "tasks",
                        principalColumns: new[] { "relocation_id", "id" },
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.AddCheckConstraint(
                name: "ck_documents_category",
                table: "documents",
                sql: "category IS NULL OR category IN ('Identity', 'Visa', 'Housing', 'Employment', 'Education', 'Insurance', 'Health', 'Finance', 'Moving', 'Other')");

            migrationBuilder.CreateIndex(
                name: "ix_document_task_links_relocation_id_document_id",
                table: "document_task_links",
                columns: new[] { "relocation_id", "document_id" });

            migrationBuilder.CreateIndex(
                name: "ix_document_task_links_relocation_id_task_id",
                table: "document_task_links",
                columns: new[] { "relocation_id", "task_id" });

            migrationBuilder.CreateIndex(
                name: "ux_document_task_links_document_id_task_id",
                table: "document_task_links",
                columns: new[] { "document_id", "task_id" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "document_task_links");

            migrationBuilder.DropUniqueConstraint(
                name: "ak_documents_relocation_id_id",
                table: "documents");

            migrationBuilder.DropCheckConstraint(
                name: "ck_documents_category",
                table: "documents");

            migrationBuilder.DropColumn(
                name: "category",
                table: "documents");

            migrationBuilder.DropColumn(
                name: "expires_on",
                table: "documents");

            migrationBuilder.DropColumn(
                name: "updated_at",
                table: "documents");
        }
    }
}
