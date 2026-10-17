using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Relovia.Api.Common.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddBudgets : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "budgets",
                columns: table => new
                {
                    relocation_id = table.Column<Guid>(type: "uuid", nullable: false),
                    currency_code = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_budgets", x => x.relocation_id);
                    table.CheckConstraint("ck_budgets_currency_code", "currency_code ~ '^[A-Z]{3}$'");
                    table.ForeignKey(
                        name: "fk_budgets_relocations_relocation_id",
                        column: x => x.relocation_id,
                        principalTable: "relocations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "budget_categories",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    relocation_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_budget_categories", x => x.id);
                    table.UniqueConstraint("ak_budget_categories_relocation_id_id", x => new { x.relocation_id, x.id });
                    table.ForeignKey(
                        name: "fk_budget_categories_budgets_relocation_id",
                        column: x => x.relocation_id,
                        principalTable: "budgets",
                        principalColumn: "relocation_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "budget_estimates",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    relocation_id = table.Column<Guid>(type: "uuid", nullable: false),
                    category_id = table.Column<Guid>(type: "uuid", nullable: false),
                    label = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    amount = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_budget_estimates", x => x.id);
                    table.CheckConstraint("ck_budget_estimates_amount", "amount > 0");
                    table.ForeignKey(
                        name: "fk_budget_estimates_budget_categories_relocation_id_category_id",
                        columns: x => new { x.relocation_id, x.category_id },
                        principalTable: "budget_categories",
                        principalColumns: new[] { "relocation_id", "id" },
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "expenses",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    relocation_id = table.Column<Guid>(type: "uuid", nullable: false),
                    category_id = table.Column<Guid>(type: "uuid", nullable: false),
                    task_id = table.Column<Guid>(type: "uuid", nullable: true),
                    description = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    amount = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    currency_code = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    paid_on = table.Column<DateOnly>(type: "date", nullable: false),
                    notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_expenses", x => x.id);
                    table.CheckConstraint("ck_expenses_amount", "amount > 0");
                    table.CheckConstraint("ck_expenses_currency_code", "currency_code ~ '^[A-Z]{3}$'");
                    table.ForeignKey(
                        name: "fk_expenses_budget_categories_relocation_id_category_id",
                        columns: x => new { x.relocation_id, x.category_id },
                        principalTable: "budget_categories",
                        principalColumns: new[] { "relocation_id", "id" });
                    table.ForeignKey(
                        name: "fk_expenses_relocation_tasks_task_id",
                        column: x => x.task_id,
                        principalTable: "tasks",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_expenses_relocations_relocation_id",
                        column: x => x.relocation_id,
                        principalTable: "relocations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ux_budget_categories_relocation_id_name",
                table: "budget_categories",
                columns: new[] { "relocation_id", "name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_budget_estimates_relocation_id_category_id",
                table: "budget_estimates",
                columns: new[] { "relocation_id", "category_id" });

            migrationBuilder.CreateIndex(
                name: "ix_expenses_relocation_id_category_id",
                table: "expenses",
                columns: new[] { "relocation_id", "category_id" });

            migrationBuilder.CreateIndex(
                name: "ix_expenses_relocation_id_paid_on",
                table: "expenses",
                columns: new[] { "relocation_id", "paid_on" });

            migrationBuilder.CreateIndex(
                name: "ix_expenses_task_id",
                table: "expenses",
                column: "task_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "budget_estimates");

            migrationBuilder.DropTable(
                name: "expenses");

            migrationBuilder.DropTable(
                name: "budget_categories");

            migrationBuilder.DropTable(
                name: "budgets");
        }
    }
}
