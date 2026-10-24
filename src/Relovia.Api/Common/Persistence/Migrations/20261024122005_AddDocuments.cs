using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Relovia.Api.Common.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddDocuments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "documents",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    relocation_id = table.Column<Guid>(type: "uuid", nullable: false),
                    storage_key = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    file_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    content_type = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    size_bytes = table.Column<long>(type: "bigint", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_documents", x => x.id);
                    table.CheckConstraint("ck_documents_content_type", "content_type IN ('application/pdf', 'image/jpeg', 'image/png', 'image/webp')");
                    table.CheckConstraint("ck_documents_file_name", "char_length(btrim(file_name)) > 0 AND char_length(file_name) <= 200");
                    table.CheckConstraint("ck_documents_size_bytes", "size_bytes > 0 AND size_bytes <= 10485760");
                    table.CheckConstraint("ck_documents_storage_key", "storage_key ~ '^[0-9a-f]{32}$'");
                    table.ForeignKey(
                        name: "fk_documents_relocations_relocation_id",
                        column: x => x.relocation_id,
                        principalTable: "relocations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_documents_relocation_id",
                table: "documents",
                column: "relocation_id");

            migrationBuilder.CreateIndex(
                name: "ux_documents_storage_key",
                table: "documents",
                column: "storage_key",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "documents");
        }
    }
}
