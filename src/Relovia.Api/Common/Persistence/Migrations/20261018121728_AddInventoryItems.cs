using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Relovia.Api.Common.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddInventoryItems : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "inventory_items",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    relocation_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    category = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    room = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    quantity = table.Column<int>(type: "integer", nullable: false),
                    acquisition = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    moving_action = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    packing_status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    priority = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    estimated_price = table.Column<decimal>(type: "numeric(18,4)", nullable: true),
                    actual_price = table.Column<decimal>(type: "numeric(18,4)", nullable: true),
                    currency_code = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: true),
                    notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_inventory_items", x => x.id);
                    table.CheckConstraint("ck_inventory_items_acquisition", "acquisition IN ('Owned', 'ToPurchase')");
                    table.CheckConstraint("ck_inventory_items_actual_price", "actual_price IS NULL OR actual_price > 0");
                    table.CheckConstraint("ck_inventory_items_category", "category IS NULL OR char_length(btrim(category)) > 0");
                    table.CheckConstraint("ck_inventory_items_currency", "(\n    estimated_price IS NULL\n    AND actual_price IS NULL\n    AND currency_code IS NULL\n) OR (\n    currency_code ~ '^[A-Z]{3}$'\n    AND (estimated_price IS NOT NULL OR actual_price IS NOT NULL)\n)");
                    table.CheckConstraint("ck_inventory_items_estimated_price", "estimated_price IS NULL OR estimated_price > 0");
                    table.CheckConstraint("ck_inventory_items_moving_action", "moving_action IS NULL OR moving_action IN ('Bring', 'Sell', 'Donate', 'Store', 'Discard')");
                    table.CheckConstraint("ck_inventory_items_name", "char_length(btrim(name)) > 0");
                    table.CheckConstraint("ck_inventory_items_notes", "notes IS NULL OR char_length(btrim(notes)) > 0");
                    table.CheckConstraint("ck_inventory_items_packing_matches_action", "(moving_action = 'Bring') = (packing_status IS NOT NULL)");
                    table.CheckConstraint("ck_inventory_items_packing_status", "packing_status IS NULL OR packing_status IN ('NotPacked', 'Packed', 'Unpacked')");
                    table.CheckConstraint("ck_inventory_items_priority", "priority IN ('Low', 'Normal', 'High')");
                    table.CheckConstraint("ck_inventory_items_quantity", "quantity >= 1 AND quantity <= 100000");
                    table.CheckConstraint("ck_inventory_items_room", "room IS NULL OR char_length(btrim(room)) > 0");
                    table.ForeignKey(
                        name: "fk_inventory_items_relocations_relocation_id",
                        column: x => x.relocation_id,
                        principalTable: "relocations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_inventory_items_relocation_id_room_name",
                table: "inventory_items",
                columns: new[] { "relocation_id", "room", "name" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "inventory_items");
        }
    }
}
