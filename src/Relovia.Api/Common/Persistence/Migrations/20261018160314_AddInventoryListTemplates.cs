using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Relovia.Api.Common.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddInventoryListTemplates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "code",
                table: "inventory_items",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "source_template_item_id",
                table: "inventory_items",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "inventory_list_templates",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    version = table.Column<int>(type: "integer", nullable: false),
                    title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    room = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    scope = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    published_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_inventory_list_templates", x => x.id);
                    table.CheckConstraint("ck_inventory_list_templates_code", "code ~ '^[A-Za-z0-9_-]+$'");
                    table.CheckConstraint("ck_inventory_list_templates_room", "room IN ('Kitchen', 'Bathroom', 'Bedroom', 'LivingRoom', 'DiningRoom', 'Office', 'Other')");
                    table.CheckConstraint("ck_inventory_list_templates_scope", "scope IN ('Furnished', 'Unfurnished', 'Both')");
                    table.CheckConstraint("ck_inventory_list_templates_title", "char_length(btrim(title)) > 0");
                    table.CheckConstraint("ck_inventory_list_templates_version", "version >= 1");
                });

            migrationBuilder.CreateTable(
                name: "inventory_template_items",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    inventory_list_template_id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    category = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    quantity = table.Column<int>(type: "integer", nullable: false),
                    priority = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_inventory_template_items", x => x.id);
                    table.CheckConstraint("ck_inventory_template_items_category", "category IN ('Furniture', 'Appliances', 'Linens', 'Cookware', 'Electronics', 'Decor', 'Other')");
                    table.CheckConstraint("ck_inventory_template_items_code", "code ~ '^[A-Za-z0-9_-]+$'");
                    table.CheckConstraint("ck_inventory_template_items_name", "char_length(btrim(name)) > 0");
                    table.CheckConstraint("ck_inventory_template_items_priority", "priority IN ('Low', 'Normal', 'High')");
                    table.CheckConstraint("ck_inventory_template_items_quantity", "quantity >= 1 AND quantity <= 100000");
                    table.ForeignKey(
                        name: "fk_inventory_template_items_inventory_list_templates_inventory",
                        column: x => x.inventory_list_template_id,
                        principalTable: "inventory_list_templates",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "inventory_list_templates",
                columns: new[] { "id", "code", "created_at", "published_at", "room", "scope", "title", "updated_at", "version" },
                values: new object[,]
                {
                    { new Guid("019a18b0-7c00-7000-8000-000000000001"), "bathroom-basics", new DateTimeOffset(new DateTime(2026, 10, 18, 15, 30, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), new DateTimeOffset(new DateTime(2026, 10, 18, 15, 30, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Bathroom", "Both", "Bathroom basics", new DateTimeOffset(new DateTime(2026, 10, 18, 15, 30, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), 1 },
                    { new Guid("019a18b0-7c00-7000-8000-000000000002"), "bedroom-basics", new DateTimeOffset(new DateTime(2026, 10, 18, 15, 30, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), new DateTimeOffset(new DateTime(2026, 10, 18, 15, 30, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Bedroom", "Both", "Bedroom basics", new DateTimeOffset(new DateTime(2026, 10, 18, 15, 30, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), 1 },
                    { new Guid("019a18b0-7c00-7000-8000-000000000003"), "kitchen-basics", new DateTimeOffset(new DateTime(2026, 10, 18, 15, 30, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), new DateTimeOffset(new DateTime(2026, 10, 18, 15, 30, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Kitchen", "Both", "Kitchen basics", new DateTimeOffset(new DateTime(2026, 10, 18, 15, 30, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), 1 },
                    { new Guid("019a18b0-7c00-7000-8000-000000000004"), "bedroom-furniture", new DateTimeOffset(new DateTime(2026, 10, 18, 15, 30, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), new DateTimeOffset(new DateTime(2026, 10, 18, 15, 30, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Bedroom", "Unfurnished", "Bedroom furniture", new DateTimeOffset(new DateTime(2026, 10, 18, 15, 30, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), 1 },
                    { new Guid("019a18b0-7c00-7000-8000-000000000005"), "kitchen-appliances", new DateTimeOffset(new DateTime(2026, 10, 18, 15, 30, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), new DateTimeOffset(new DateTime(2026, 10, 18, 15, 30, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Kitchen", "Unfurnished", "Kitchen appliances", new DateTimeOffset(new DateTime(2026, 10, 18, 15, 30, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), 1 },
                    { new Guid("019a18b0-7c00-7000-8000-000000000006"), "living-room", new DateTimeOffset(new DateTime(2026, 10, 18, 15, 30, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), new DateTimeOffset(new DateTime(2026, 10, 18, 15, 30, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "LivingRoom", "Unfurnished", "Living room", new DateTimeOffset(new DateTime(2026, 10, 18, 15, 30, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), 1 },
                    { new Guid("019a18b0-7c00-7000-8000-000000000007"), "dining-room", new DateTimeOffset(new DateTime(2026, 10, 18, 15, 30, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), new DateTimeOffset(new DateTime(2026, 10, 18, 15, 30, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "DiningRoom", "Unfurnished", "Dining room", new DateTimeOffset(new DateTime(2026, 10, 18, 15, 30, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), 1 },
                    { new Guid("019a18b0-7c00-7000-8000-000000000008"), "office", new DateTimeOffset(new DateTime(2026, 10, 18, 15, 30, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), new DateTimeOffset(new DateTime(2026, 10, 18, 15, 30, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Office", "Unfurnished", "Office", new DateTimeOffset(new DateTime(2026, 10, 18, 15, 30, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), 1 }
                });

            migrationBuilder.InsertData(
                table: "inventory_template_items",
                columns: new[] { "id", "category", "code", "created_at", "inventory_list_template_id", "name", "priority", "quantity", "updated_at" },
                values: new object[,]
                {
                    { new Guid("019a18b0-7c00-7000-8000-000000000101"), "Linens", "bath-towels", new DateTimeOffset(new DateTime(2026, 10, 18, 15, 30, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), new Guid("019a18b0-7c00-7000-8000-000000000001"), "Bath towels", "Normal", 4, new DateTimeOffset(new DateTime(2026, 10, 18, 15, 30, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("019a18b0-7c00-7000-8000-000000000102"), "Linens", "hand-towels", new DateTimeOffset(new DateTime(2026, 10, 18, 15, 30, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), new Guid("019a18b0-7c00-7000-8000-000000000001"), "Hand towels", "Normal", 2, new DateTimeOffset(new DateTime(2026, 10, 18, 15, 30, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("019a18b0-7c00-7000-8000-000000000103"), "Other", "toilet-brush", new DateTimeOffset(new DateTime(2026, 10, 18, 15, 30, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), new Guid("019a18b0-7c00-7000-8000-000000000001"), "Toilet brush", "Normal", 1, new DateTimeOffset(new DateTime(2026, 10, 18, 15, 30, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("019a18b0-7c00-7000-8000-000000000104"), "Linens", "bath-mat", new DateTimeOffset(new DateTime(2026, 10, 18, 15, 30, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), new Guid("019a18b0-7c00-7000-8000-000000000001"), "Bath mat", "Low", 1, new DateTimeOffset(new DateTime(2026, 10, 18, 15, 30, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("019a18b0-7c00-7000-8000-000000000111"), "Linens", "bed-sheets", new DateTimeOffset(new DateTime(2026, 10, 18, 15, 30, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), new Guid("019a18b0-7c00-7000-8000-000000000002"), "Bed sheets", "High", 2, new DateTimeOffset(new DateTime(2026, 10, 18, 15, 30, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("019a18b0-7c00-7000-8000-000000000112"), "Linens", "duvet", new DateTimeOffset(new DateTime(2026, 10, 18, 15, 30, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), new Guid("019a18b0-7c00-7000-8000-000000000002"), "Duvet", "High", 1, new DateTimeOffset(new DateTime(2026, 10, 18, 15, 30, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("019a18b0-7c00-7000-8000-000000000113"), "Linens", "pillows", new DateTimeOffset(new DateTime(2026, 10, 18, 15, 30, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), new Guid("019a18b0-7c00-7000-8000-000000000002"), "Pillows", "High", 2, new DateTimeOffset(new DateTime(2026, 10, 18, 15, 30, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("019a18b0-7c00-7000-8000-000000000114"), "Other", "hangers", new DateTimeOffset(new DateTime(2026, 10, 18, 15, 30, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), new Guid("019a18b0-7c00-7000-8000-000000000002"), "Hangers", "Normal", 20, new DateTimeOffset(new DateTime(2026, 10, 18, 15, 30, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("019a18b0-7c00-7000-8000-000000000115"), "Other", "laundry-basket", new DateTimeOffset(new DateTime(2026, 10, 18, 15, 30, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), new Guid("019a18b0-7c00-7000-8000-000000000002"), "Laundry basket", "Low", 1, new DateTimeOffset(new DateTime(2026, 10, 18, 15, 30, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("019a18b0-7c00-7000-8000-000000000121"), "Cookware", "frying-pan", new DateTimeOffset(new DateTime(2026, 10, 18, 15, 30, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), new Guid("019a18b0-7c00-7000-8000-000000000003"), "Frying pan", "High", 1, new DateTimeOffset(new DateTime(2026, 10, 18, 15, 30, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("019a18b0-7c00-7000-8000-000000000122"), "Cookware", "saucepan", new DateTimeOffset(new DateTime(2026, 10, 18, 15, 30, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), new Guid("019a18b0-7c00-7000-8000-000000000003"), "Saucepan", "High", 1, new DateTimeOffset(new DateTime(2026, 10, 18, 15, 30, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("019a18b0-7c00-7000-8000-000000000123"), "Cookware", "knife", new DateTimeOffset(new DateTime(2026, 10, 18, 15, 30, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), new Guid("019a18b0-7c00-7000-8000-000000000003"), "Chef's knife", "High", 1, new DateTimeOffset(new DateTime(2026, 10, 18, 15, 30, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("019a18b0-7c00-7000-8000-000000000124"), "Cookware", "cutting-board", new DateTimeOffset(new DateTime(2026, 10, 18, 15, 30, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), new Guid("019a18b0-7c00-7000-8000-000000000003"), "Cutting board", "Normal", 1, new DateTimeOffset(new DateTime(2026, 10, 18, 15, 30, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("019a18b0-7c00-7000-8000-000000000125"), "Cookware", "plates", new DateTimeOffset(new DateTime(2026, 10, 18, 15, 30, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), new Guid("019a18b0-7c00-7000-8000-000000000003"), "Plates", "Normal", 4, new DateTimeOffset(new DateTime(2026, 10, 18, 15, 30, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("019a18b0-7c00-7000-8000-000000000126"), "Cookware", "bowls", new DateTimeOffset(new DateTime(2026, 10, 18, 15, 30, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), new Guid("019a18b0-7c00-7000-8000-000000000003"), "Bowls", "Normal", 4, new DateTimeOffset(new DateTime(2026, 10, 18, 15, 30, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("019a18b0-7c00-7000-8000-000000000127"), "Cookware", "glasses", new DateTimeOffset(new DateTime(2026, 10, 18, 15, 30, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), new Guid("019a18b0-7c00-7000-8000-000000000003"), "Glasses", "Normal", 4, new DateTimeOffset(new DateTime(2026, 10, 18, 15, 30, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("019a18b0-7c00-7000-8000-000000000128"), "Cookware", "cutlery", new DateTimeOffset(new DateTime(2026, 10, 18, 15, 30, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), new Guid("019a18b0-7c00-7000-8000-000000000003"), "Cutlery", "Normal", 1, new DateTimeOffset(new DateTime(2026, 10, 18, 15, 30, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("019a18b0-7c00-7000-8000-000000000129"), "Appliances", "kettle", new DateTimeOffset(new DateTime(2026, 10, 18, 15, 30, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), new Guid("019a18b0-7c00-7000-8000-000000000003"), "Kettle", "Normal", 1, new DateTimeOffset(new DateTime(2026, 10, 18, 15, 30, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("019a18b0-7c00-7000-8000-000000000130"), "Other", "bin", new DateTimeOffset(new DateTime(2026, 10, 18, 15, 30, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), new Guid("019a18b0-7c00-7000-8000-000000000003"), "Bin", "Normal", 1, new DateTimeOffset(new DateTime(2026, 10, 18, 15, 30, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("019a18b0-7c00-7000-8000-000000000131"), "Linens", "tea-towels", new DateTimeOffset(new DateTime(2026, 10, 18, 15, 30, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), new Guid("019a18b0-7c00-7000-8000-000000000003"), "Tea towels", "Low", 2, new DateTimeOffset(new DateTime(2026, 10, 18, 15, 30, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("019a18b0-7c00-7000-8000-000000000141"), "Furniture", "bed-frame", new DateTimeOffset(new DateTime(2026, 10, 18, 15, 30, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), new Guid("019a18b0-7c00-7000-8000-000000000004"), "Bed frame", "High", 1, new DateTimeOffset(new DateTime(2026, 10, 18, 15, 30, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("019a18b0-7c00-7000-8000-000000000142"), "Furniture", "mattress", new DateTimeOffset(new DateTime(2026, 10, 18, 15, 30, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), new Guid("019a18b0-7c00-7000-8000-000000000004"), "Mattress", "High", 1, new DateTimeOffset(new DateTime(2026, 10, 18, 15, 30, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("019a18b0-7c00-7000-8000-000000000143"), "Furniture", "wardrobe", new DateTimeOffset(new DateTime(2026, 10, 18, 15, 30, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), new Guid("019a18b0-7c00-7000-8000-000000000004"), "Wardrobe", "Normal", 1, new DateTimeOffset(new DateTime(2026, 10, 18, 15, 30, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("019a18b0-7c00-7000-8000-000000000151"), "Appliances", "fridge", new DateTimeOffset(new DateTime(2026, 10, 18, 15, 30, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), new Guid("019a18b0-7c00-7000-8000-000000000005"), "Fridge", "High", 1, new DateTimeOffset(new DateTime(2026, 10, 18, 15, 30, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("019a18b0-7c00-7000-8000-000000000152"), "Appliances", "cooker", new DateTimeOffset(new DateTime(2026, 10, 18, 15, 30, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), new Guid("019a18b0-7c00-7000-8000-000000000005"), "Cooker", "High", 1, new DateTimeOffset(new DateTime(2026, 10, 18, 15, 30, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("019a18b0-7c00-7000-8000-000000000153"), "Appliances", "microwave", new DateTimeOffset(new DateTime(2026, 10, 18, 15, 30, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), new Guid("019a18b0-7c00-7000-8000-000000000005"), "Microwave", "Low", 1, new DateTimeOffset(new DateTime(2026, 10, 18, 15, 30, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("019a18b0-7c00-7000-8000-000000000161"), "Furniture", "sofa", new DateTimeOffset(new DateTime(2026, 10, 18, 15, 30, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), new Guid("019a18b0-7c00-7000-8000-000000000006"), "Sofa", "High", 1, new DateTimeOffset(new DateTime(2026, 10, 18, 15, 30, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("019a18b0-7c00-7000-8000-000000000162"), "Furniture", "coffee-table", new DateTimeOffset(new DateTime(2026, 10, 18, 15, 30, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), new Guid("019a18b0-7c00-7000-8000-000000000006"), "Coffee table", "Normal", 1, new DateTimeOffset(new DateTime(2026, 10, 18, 15, 30, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("019a18b0-7c00-7000-8000-000000000163"), "Decor", "floor-lamp", new DateTimeOffset(new DateTime(2026, 10, 18, 15, 30, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), new Guid("019a18b0-7c00-7000-8000-000000000006"), "Floor lamp", "Low", 1, new DateTimeOffset(new DateTime(2026, 10, 18, 15, 30, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("019a18b0-7c00-7000-8000-000000000164"), "Decor", "curtains", new DateTimeOffset(new DateTime(2026, 10, 18, 15, 30, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), new Guid("019a18b0-7c00-7000-8000-000000000006"), "Curtains", "Normal", 1, new DateTimeOffset(new DateTime(2026, 10, 18, 15, 30, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("019a18b0-7c00-7000-8000-000000000171"), "Furniture", "dining-table", new DateTimeOffset(new DateTime(2026, 10, 18, 15, 30, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), new Guid("019a18b0-7c00-7000-8000-000000000007"), "Dining table", "Normal", 1, new DateTimeOffset(new DateTime(2026, 10, 18, 15, 30, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("019a18b0-7c00-7000-8000-000000000172"), "Furniture", "dining-chairs", new DateTimeOffset(new DateTime(2026, 10, 18, 15, 30, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), new Guid("019a18b0-7c00-7000-8000-000000000007"), "Dining chairs", "Normal", 4, new DateTimeOffset(new DateTime(2026, 10, 18, 15, 30, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("019a18b0-7c00-7000-8000-000000000181"), "Furniture", "desk", new DateTimeOffset(new DateTime(2026, 10, 18, 15, 30, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), new Guid("019a18b0-7c00-7000-8000-000000000008"), "Desk", "Normal", 1, new DateTimeOffset(new DateTime(2026, 10, 18, 15, 30, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("019a18b0-7c00-7000-8000-000000000182"), "Furniture", "desk-chair", new DateTimeOffset(new DateTime(2026, 10, 18, 15, 30, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), new Guid("019a18b0-7c00-7000-8000-000000000008"), "Desk chair", "Normal", 1, new DateTimeOffset(new DateTime(2026, 10, 18, 15, 30, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) }
                });

            migrationBuilder.CreateIndex(
                name: "ix_inventory_items_source_template_item_id",
                table: "inventory_items",
                column: "source_template_item_id");

            migrationBuilder.CreateIndex(
                name: "ux_inventory_items_relocation_id_code",
                table: "inventory_items",
                columns: new[] { "relocation_id", "code" },
                unique: true,
                filter: "code IS NOT NULL");

            migrationBuilder.AddCheckConstraint(
                name: "ck_inventory_items_code",
                table: "inventory_items",
                sql: "code IS NULL OR code ~ '^[A-Za-z0-9_-]+$'");

            migrationBuilder.CreateIndex(
                name: "ux_inventory_list_templates_code_version",
                table: "inventory_list_templates",
                columns: new[] { "code", "version" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_inventory_template_items_template_id_code",
                table: "inventory_template_items",
                columns: new[] { "inventory_list_template_id", "code" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "fk_inventory_items_inventory_template_items_source_template_it",
                table: "inventory_items",
                column: "source_template_item_id",
                principalTable: "inventory_template_items",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_inventory_items_inventory_template_items_source_template_it",
                table: "inventory_items");

            migrationBuilder.DropTable(
                name: "inventory_template_items");

            migrationBuilder.DropTable(
                name: "inventory_list_templates");

            migrationBuilder.DropIndex(
                name: "ix_inventory_items_source_template_item_id",
                table: "inventory_items");

            migrationBuilder.DropIndex(
                name: "ux_inventory_items_relocation_id_code",
                table: "inventory_items");

            migrationBuilder.DropCheckConstraint(
                name: "ck_inventory_items_code",
                table: "inventory_items");

            migrationBuilder.DropColumn(
                name: "code",
                table: "inventory_items");

            migrationBuilder.DropColumn(
                name: "source_template_item_id",
                table: "inventory_items");
        }
    }
}
