using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Relovia.Api.Common.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddMonthlyNetIncomeAndRentPeriod : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "rent_period",
                table: "housing_options",
                type: "character varying(32)",
                maxLength: 32,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "monthly_net_income",
                table: "budgets",
                type: "numeric(18,4)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "monthly_net_income_currency",
                table: "budgets",
                type: "character varying(3)",
                maxLength: 3,
                nullable: true);

            // Rents already stored are monthly.
            migrationBuilder.Sql(
                """
                UPDATE housing_options
                SET rent_period = 'Monthly'
                WHERE rent IS NOT NULL;
                """);

            migrationBuilder.AddCheckConstraint(
                name: "ck_housing_options_rent_period",
                table: "housing_options",
                sql: "(\n    rent IS NULL\n    AND rent_period IS NULL\n) OR (\n    rent IS NOT NULL\n    AND rent_period IN ('Weekly', 'Monthly')\n)");

            migrationBuilder.AddCheckConstraint(
                name: "ck_budgets_monthly_net_income",
                table: "budgets",
                sql: "(\n    monthly_net_income IS NULL\n    AND monthly_net_income_currency IS NULL\n) OR (\n    monthly_net_income > 0\n    AND monthly_net_income_currency ~ '^[A-Z]{3}$'\n)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_housing_options_rent_period",
                table: "housing_options");

            migrationBuilder.DropCheckConstraint(
                name: "ck_budgets_monthly_net_income",
                table: "budgets");

            migrationBuilder.DropColumn(
                name: "rent_period",
                table: "housing_options");

            migrationBuilder.DropColumn(
                name: "monthly_net_income",
                table: "budgets");

            migrationBuilder.DropColumn(
                name: "monthly_net_income_currency",
                table: "budgets");
        }
    }
}
