using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Relovia.Api.Common.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddHousingAndJobApplications : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "housing_options",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    relocation_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    area = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    rent = table.Column<decimal>(type: "numeric(18,4)", nullable: true),
                    deposit = table.Column<decimal>(type: "numeric(18,4)", nullable: true),
                    currency_code = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: true),
                    size = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    furnishing = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    available_on = table.Column<DateOnly>(type: "date", nullable: true),
                    status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_housing_options", x => x.id);
                    table.CheckConstraint("ck_housing_options_area", "area IS NULL OR (char_length(btrim(area)) > 0 AND char_length(area) <= 200)");
                    table.CheckConstraint("ck_housing_options_currency", "(\n    rent IS NULL\n    AND deposit IS NULL\n    AND currency_code IS NULL\n) OR (\n    currency_code ~ '^[A-Z]{3}$'\n    AND (rent IS NOT NULL OR deposit IS NOT NULL)\n)");
                    table.CheckConstraint("ck_housing_options_deposit", "deposit IS NULL OR deposit > 0");
                    table.CheckConstraint("ck_housing_options_furnishing", "furnishing IS NULL OR furnishing IN ('Furnished', 'Unfurnished')");
                    table.CheckConstraint("ck_housing_options_name", "char_length(btrim(name)) > 0 AND char_length(name) <= 200");
                    table.CheckConstraint("ck_housing_options_notes", "notes IS NULL OR (char_length(btrim(notes)) > 0 AND char_length(notes) <= 1000)");
                    table.CheckConstraint("ck_housing_options_rent", "rent IS NULL OR rent > 0");
                    table.CheckConstraint("ck_housing_options_size", "size IS NULL OR (char_length(btrim(size)) > 0 AND char_length(size) <= 80)");
                    table.CheckConstraint("ck_housing_options_status", "status IN ('Considering', 'Viewing', 'Offered', 'Chosen', 'Dropped')");
                    table.ForeignKey(
                        name: "fk_housing_options_relocations_relocation_id",
                        column: x => x.relocation_id,
                        principalTable: "relocations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "job_applications",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    relocation_id = table.Column<Guid>(type: "uuid", nullable: false),
                    employer = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    role = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    location = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    applied_on = table.Column<DateOnly>(type: "date", nullable: true),
                    next_on = table.Column<DateOnly>(type: "date", nullable: true),
                    status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    includes_support = table.Column<bool>(type: "boolean", nullable: true),
                    notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_job_applications", x => x.id);
                    table.CheckConstraint("ck_job_applications_employer", "char_length(btrim(employer)) > 0 AND char_length(employer) <= 200");
                    table.CheckConstraint("ck_job_applications_location", "location IS NULL OR (char_length(btrim(location)) > 0 AND char_length(location) <= 200)");
                    table.CheckConstraint("ck_job_applications_notes", "notes IS NULL OR (char_length(btrim(notes)) > 0 AND char_length(notes) <= 1000)");
                    table.CheckConstraint("ck_job_applications_role", "char_length(btrim(role)) > 0 AND char_length(role) <= 200");
                    table.CheckConstraint("ck_job_applications_status", "status IN ('Applied', 'Interviewing', 'Offer', 'Accepted', 'Rejected', 'Withdrawn')");
                    table.ForeignKey(
                        name: "fk_job_applications_relocations_relocation_id",
                        column: x => x.relocation_id,
                        principalTable: "relocations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ux_housing_options_one_chosen",
                table: "housing_options",
                column: "relocation_id",
                unique: true,
                filter: "status = 'Chosen'");

            migrationBuilder.CreateIndex(
                name: "ix_job_applications_relocation_id",
                table: "job_applications",
                column: "relocation_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "housing_options");

            migrationBuilder.DropTable(
                name: "job_applications");
        }
    }
}
