using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Relovia.Api.Common.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ConstrainInventoryLists : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_inventory_items_category",
                table: "inventory_items");

            migrationBuilder.DropCheckConstraint(
                name: "ck_inventory_items_room",
                table: "inventory_items");

            // Free-text values from the first inventory version become the fixed lists, or empty.
            migrationBuilder.Sql(
                """
                UPDATE inventory_items
                SET room = CASE lower(btrim(room))
                    WHEN 'kitchen' THEN 'Kitchen'
                    WHEN 'bathroom' THEN 'Bathroom'
                    WHEN 'bedroom' THEN 'Bedroom'
                    WHEN 'living room' THEN 'LivingRoom'
                    WHEN 'livingroom' THEN 'LivingRoom'
                    WHEN 'dining room' THEN 'DiningRoom'
                    WHEN 'diningroom' THEN 'DiningRoom'
                    WHEN 'office' THEN 'Office'
                    WHEN 'other' THEN 'Other'
                    ELSE NULL
                END
                WHERE room IS NOT NULL;

                UPDATE inventory_items
                SET category = CASE lower(btrim(category))
                    WHEN 'furniture' THEN 'Furniture'
                    WHEN 'appliances' THEN 'Appliances'
                    WHEN 'linens' THEN 'Linens'
                    WHEN 'cookware' THEN 'Cookware'
                    WHEN 'electronics' THEN 'Electronics'
                    WHEN 'decor' THEN 'Decor'
                    WHEN 'other' THEN 'Other'
                    ELSE NULL
                END
                WHERE category IS NOT NULL;
                """);

            migrationBuilder.AlterColumn<string>(
                name: "room",
                table: "inventory_items",
                type: "character varying(32)",
                maxLength: 32,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "category",
                table: "inventory_items",
                type: "character varying(32)",
                maxLength: 32,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100,
                oldNullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "budget_category_id",
                table: "inventory_items",
                type: "uuid",
                nullable: true);

            // A price already stored on something to buy lands in Furnishing when that category exists.
            migrationBuilder.Sql(
                """
                UPDATE inventory_items AS item
                SET budget_category_id = category.id
                FROM budget_categories AS category
                WHERE item.acquisition = 'ToPurchase'
                  AND item.budget_category_id IS NULL
                  AND (item.estimated_price IS NOT NULL OR item.actual_price IS NOT NULL)
                  AND category.relocation_id = item.relocation_id
                  AND category.name = 'Furnishing';

                UPDATE inventory_items
                SET estimated_price = NULL,
                    actual_price = NULL,
                    currency_code = NULL
                WHERE acquisition = 'ToPurchase'
                  AND budget_category_id IS NULL
                  AND (estimated_price IS NOT NULL OR actual_price IS NOT NULL);
                """);

            migrationBuilder.CreateIndex(
                name: "ix_inventory_items_budget_category_id",
                table: "inventory_items",
                column: "budget_category_id");

            migrationBuilder.AddCheckConstraint(
                name: "ck_inventory_items_budget_category",
                table: "inventory_items",
                sql: "budget_category_id IS NULL OR acquisition = 'ToPurchase'");

            migrationBuilder.AddCheckConstraint(
                name: "ck_inventory_items_category",
                table: "inventory_items",
                sql: "category IS NULL OR category IN ('Furniture', 'Appliances', 'Linens', 'Cookware', 'Electronics', 'Decor', 'Other')");

            migrationBuilder.AddCheckConstraint(
                name: "ck_inventory_items_priced_shopping",
                table: "inventory_items",
                sql: "acquisition <> 'ToPurchase' OR (estimated_price IS NULL AND actual_price IS NULL) OR budget_category_id IS NOT NULL");

            migrationBuilder.AddCheckConstraint(
                name: "ck_inventory_items_room",
                table: "inventory_items",
                sql: "room IS NULL OR room IN ('Kitchen', 'Bathroom', 'Bedroom', 'LivingRoom', 'DiningRoom', 'Office', 'Other')");

            migrationBuilder.AddForeignKey(
                name: "fk_inventory_items_budget_categories_budget_category_id",
                table: "inventory_items",
                column: "budget_category_id",
                principalTable: "budget_categories",
                principalColumn: "id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_inventory_items_budget_categories_budget_category_id",
                table: "inventory_items");

            migrationBuilder.DropIndex(
                name: "ix_inventory_items_budget_category_id",
                table: "inventory_items");

            migrationBuilder.DropCheckConstraint(
                name: "ck_inventory_items_budget_category",
                table: "inventory_items");

            migrationBuilder.DropCheckConstraint(
                name: "ck_inventory_items_category",
                table: "inventory_items");

            migrationBuilder.DropCheckConstraint(
                name: "ck_inventory_items_priced_shopping",
                table: "inventory_items");

            migrationBuilder.DropCheckConstraint(
                name: "ck_inventory_items_room",
                table: "inventory_items");

            migrationBuilder.DropColumn(
                name: "budget_category_id",
                table: "inventory_items");

            migrationBuilder.AlterColumn<string>(
                name: "room",
                table: "inventory_items",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(32)",
                oldMaxLength: 32,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "category",
                table: "inventory_items",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(32)",
                oldMaxLength: 32,
                oldNullable: true);

            migrationBuilder.AddCheckConstraint(
                name: "ck_inventory_items_category",
                table: "inventory_items",
                sql: "category IS NULL OR char_length(btrim(category)) > 0");

            migrationBuilder.AddCheckConstraint(
                name: "ck_inventory_items_room",
                table: "inventory_items",
                sql: "room IS NULL OR char_length(btrim(room)) > 0");
        }
    }
}
