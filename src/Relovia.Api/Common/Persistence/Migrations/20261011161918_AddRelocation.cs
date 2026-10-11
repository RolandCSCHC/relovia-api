using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Relovia.Api.Common.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddRelocation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "relocations",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    origin_country_code = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: false),
                    origin_city = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    destination_country_code = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: false),
                    destination_city = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    target_date = table.Column<DateOnly>(type: "date", nullable: false),
                    purpose = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false, defaultValue: "Draft"),
                    created_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_relocations", x => x.id);
                    table.CheckConstraint("ck_relocations_destination_country_code", "destination_country_code ~ '^[A-Z]{2}$'");
                    table.CheckConstraint("ck_relocations_origin_country_code", "origin_country_code ~ '^[A-Z]{2}$'");
                    table.ForeignKey(
                        name: "fk_relocations_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_relocations_user_id",
                table: "relocations",
                column: "user_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "relocations");
        }
    }
}
