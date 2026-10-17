using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Relovia.Api.Common.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddEstimateCurrency : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "currency_code",
                table: "budget_estimates",
                type: "character varying(3)",
                maxLength: 3,
                nullable: false,
                defaultValue: "");

            // Estimates written before this column were in the budget currency.
            migrationBuilder.Sql(
                """
                UPDATE budget_estimates AS estimate
                SET currency_code = budget.currency_code
                FROM budgets AS budget
                WHERE budget.relocation_id = estimate.relocation_id;
                """);

            migrationBuilder.AddCheckConstraint(
                name: "ck_budget_estimates_currency_code",
                table: "budget_estimates",
                sql: "currency_code ~ '^[A-Z]{3}$'");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_budget_estimates_currency_code",
                table: "budget_estimates");

            migrationBuilder.DropColumn(
                name: "currency_code",
                table: "budget_estimates");
        }
    }
}
