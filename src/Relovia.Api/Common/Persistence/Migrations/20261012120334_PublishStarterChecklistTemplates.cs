using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Relovia.Api.Common.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class PublishStarterChecklistTemplates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "checklist_templates",
                columns: new[] { "id", "code", "created_at", "destination_country_code", "published_at", "purpose", "scope", "title", "updated_at", "version" },
                values: new object[,]
                {
                    { new Guid("0199f0a1-7c00-7000-8000-000000000001"), "domestic-core", new DateTimeOffset(new DateTime(2026, 10, 12, 12, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, new DateTimeOffset(new DateTime(2026, 10, 12, 12, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, "Domestic", "Domestic move", new DateTimeOffset(new DateTime(2026, 10, 12, 12, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), 1 },
                    { new Guid("0199f0a1-7c00-7000-8000-000000000002"), "international-core", new DateTimeOffset(new DateTime(2026, 10, 12, 12, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, new DateTimeOffset(new DateTime(2026, 10, 12, 12, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, "International", "International move", new DateTimeOffset(new DateTime(2026, 10, 12, 12, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), 1 }
                });

            migrationBuilder.InsertData(
                table: "task_templates",
                columns: new[] { "id", "checklist_template_id", "code", "created_at", "days_before_move", "phase", "priority", "title", "updated_at" },
                values: new object[,]
                {
                    { new Guid("0199f0a1-7c00-7000-8000-000000000010"), new Guid("0199f0a1-7c00-7000-8000-000000000001"), "give-notice", new DateTimeOffset(new DateTime(2026, 10, 12, 12, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), 60, "Preparation", "High", "Give notice on your current home", new DateTimeOffset(new DateTime(2026, 10, 12, 12, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("0199f0a1-7c00-7000-8000-000000000011"), new Guid("0199f0a1-7c00-7000-8000-000000000001"), "book-movers", new DateTimeOffset(new DateTime(2026, 10, 12, 12, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), 30, "Moving", "High", "Book movers or a van", new DateTimeOffset(new DateTime(2026, 10, 12, 12, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("0199f0a1-7c00-7000-8000-000000000012"), new Guid("0199f0a1-7c00-7000-8000-000000000001"), "transfer-utilities", new DateTimeOffset(new DateTime(2026, 10, 12, 12, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), 14, "Housing", "Normal", "Set up utilities at the new home", new DateTimeOffset(new DateTime(2026, 10, 12, 12, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("0199f0a1-7c00-7000-8000-000000000013"), new Guid("0199f0a1-7c00-7000-8000-000000000001"), "change-address", new DateTimeOffset(new DateTime(2026, 10, 12, 12, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), 7, "Administration", "Normal", "Update your address", new DateTimeOffset(new DateTime(2026, 10, 12, 12, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("0199f0a1-7c00-7000-8000-000000000014"), new Guid("0199f0a1-7c00-7000-8000-000000000001"), "unpack-essentials", new DateTimeOffset(new DateTime(2026, 10, 12, 12, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), -1, "Settlement", "Normal", "Unpack the essentials", new DateTimeOffset(new DateTime(2026, 10, 12, 12, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("0199f0a1-7c00-7000-8000-000000000020"), new Guid("0199f0a1-7c00-7000-8000-000000000002"), "check-passport", new DateTimeOffset(new DateTime(2026, 10, 12, 12, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), 120, "Preparation", "High", "Check that your passport is valid", new DateTimeOffset(new DateTime(2026, 10, 12, 12, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("0199f0a1-7c00-7000-8000-000000000021"), new Guid("0199f0a1-7c00-7000-8000-000000000002"), "give-notice", new DateTimeOffset(new DateTime(2026, 10, 12, 12, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), 60, "Preparation", "High", "Give notice on your current home", new DateTimeOffset(new DateTime(2026, 10, 12, 12, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("0199f0a1-7c00-7000-8000-000000000022"), new Guid("0199f0a1-7c00-7000-8000-000000000002"), "arrange-housing", new DateTimeOffset(new DateTime(2026, 10, 12, 12, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), 45, "Housing", "High", "Arrange the first place to stay", new DateTimeOffset(new DateTime(2026, 10, 12, 12, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("0199f0a1-7c00-7000-8000-000000000023"), new Guid("0199f0a1-7c00-7000-8000-000000000002"), "notify-bank", new DateTimeOffset(new DateTime(2026, 10, 12, 12, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), 30, "Finances", "Normal", "Tell your bank you are moving", new DateTimeOffset(new DateTime(2026, 10, 12, 12, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("0199f0a1-7c00-7000-8000-000000000024"), new Guid("0199f0a1-7c00-7000-8000-000000000002"), "book-travel", new DateTimeOffset(new DateTime(2026, 10, 12, 12, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), 21, "Moving", "High", "Book travel for the move", new DateTimeOffset(new DateTime(2026, 10, 12, 12, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("0199f0a1-7c00-7000-8000-000000000025"), new Guid("0199f0a1-7c00-7000-8000-000000000002"), "register-arrival", new DateTimeOffset(new DateTime(2026, 10, 12, 12, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), -14, "Settlement", "High", "Register your arrival locally", new DateTimeOffset(new DateTime(2026, 10, 12, 12, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) }
                });

            migrationBuilder.InsertData(
                table: "task_template_dependencies",
                columns: new[] { "id", "depends_on_code", "task_template_id" },
                values: new object[,]
                {
                    { new Guid("0199f0a1-7c00-7000-8000-000000000101"), "give-notice", new Guid("0199f0a1-7c00-7000-8000-000000000011") },
                    { new Guid("0199f0a1-7c00-7000-8000-000000000102"), "book-movers", new Guid("0199f0a1-7c00-7000-8000-000000000014") },
                    { new Guid("0199f0a1-7c00-7000-8000-000000000103"), "check-passport", new Guid("0199f0a1-7c00-7000-8000-000000000024") },
                    { new Guid("0199f0a1-7c00-7000-8000-000000000104"), "arrange-housing", new Guid("0199f0a1-7c00-7000-8000-000000000025") }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "task_template_dependencies",
                keyColumn: "id",
                keyValue: new Guid("0199f0a1-7c00-7000-8000-000000000101"));

            migrationBuilder.DeleteData(
                table: "task_template_dependencies",
                keyColumn: "id",
                keyValue: new Guid("0199f0a1-7c00-7000-8000-000000000102"));

            migrationBuilder.DeleteData(
                table: "task_template_dependencies",
                keyColumn: "id",
                keyValue: new Guid("0199f0a1-7c00-7000-8000-000000000103"));

            migrationBuilder.DeleteData(
                table: "task_template_dependencies",
                keyColumn: "id",
                keyValue: new Guid("0199f0a1-7c00-7000-8000-000000000104"));

            migrationBuilder.DeleteData(
                table: "task_templates",
                keyColumn: "id",
                keyValue: new Guid("0199f0a1-7c00-7000-8000-000000000010"));

            migrationBuilder.DeleteData(
                table: "task_templates",
                keyColumn: "id",
                keyValue: new Guid("0199f0a1-7c00-7000-8000-000000000012"));

            migrationBuilder.DeleteData(
                table: "task_templates",
                keyColumn: "id",
                keyValue: new Guid("0199f0a1-7c00-7000-8000-000000000013"));

            migrationBuilder.DeleteData(
                table: "task_templates",
                keyColumn: "id",
                keyValue: new Guid("0199f0a1-7c00-7000-8000-000000000020"));

            migrationBuilder.DeleteData(
                table: "task_templates",
                keyColumn: "id",
                keyValue: new Guid("0199f0a1-7c00-7000-8000-000000000021"));

            migrationBuilder.DeleteData(
                table: "task_templates",
                keyColumn: "id",
                keyValue: new Guid("0199f0a1-7c00-7000-8000-000000000022"));

            migrationBuilder.DeleteData(
                table: "task_templates",
                keyColumn: "id",
                keyValue: new Guid("0199f0a1-7c00-7000-8000-000000000023"));

            migrationBuilder.DeleteData(
                table: "task_templates",
                keyColumn: "id",
                keyValue: new Guid("0199f0a1-7c00-7000-8000-000000000011"));

            migrationBuilder.DeleteData(
                table: "task_templates",
                keyColumn: "id",
                keyValue: new Guid("0199f0a1-7c00-7000-8000-000000000014"));

            migrationBuilder.DeleteData(
                table: "task_templates",
                keyColumn: "id",
                keyValue: new Guid("0199f0a1-7c00-7000-8000-000000000024"));

            migrationBuilder.DeleteData(
                table: "task_templates",
                keyColumn: "id",
                keyValue: new Guid("0199f0a1-7c00-7000-8000-000000000025"));

            migrationBuilder.DeleteData(
                table: "checklist_templates",
                keyColumn: "id",
                keyValue: new Guid("0199f0a1-7c00-7000-8000-000000000001"));

            migrationBuilder.DeleteData(
                table: "checklist_templates",
                keyColumn: "id",
                keyValue: new Guid("0199f0a1-7c00-7000-8000-000000000002"));
        }
    }
}
