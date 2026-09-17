using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tameru.Ledger.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCategorizationRules : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "categorization_rules",
                schema: "ledger",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    priority = table.Column<int>(type: "integer", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    match_field = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    match_operator = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    pattern = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    target_category_id = table.Column<Guid>(type: "uuid", nullable: true),
                    target_budget_category_id = table.Column<Guid>(type: "uuid", nullable: true),
                    target_sub_category_id = table.Column<Guid>(type: "uuid", nullable: true),
                    target_status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    deleted_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_categorization_rules", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_categorization_rules_is_active",
                schema: "ledger",
                table: "categorization_rules",
                column: "is_active");

            migrationBuilder.CreateIndex(
                name: "ix_categorization_rules_priority",
                schema: "ledger",
                table: "categorization_rules",
                column: "priority");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "categorization_rules",
                schema: "ledger");
        }
    }
}
