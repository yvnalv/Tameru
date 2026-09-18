using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tameru.Ledger.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddRuleGroupsAndAuditLog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "group_id",
                schema: "ledger",
                table: "categorization_rules",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_template",
                schema: "ledger",
                table: "categorization_rules",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "schedule_expression",
                schema: "ledger",
                table: "categorization_rules",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "tags",
                schema: "ledger",
                table: "categorization_rules",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "rule_audit_logs",
                schema: "ledger",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    rule_id = table.Column<Guid>(type: "uuid", nullable: false),
                    rule_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    transaction_id = table.Column<Guid>(type: "uuid", nullable: true),
                    transaction_title = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    amount = table.Column<decimal>(type: "numeric(19,2)", nullable: true),
                    matched_field = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    matched_value = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    was_applied = table.Column<bool>(type: "boolean", nullable: false),
                    details = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    evaluated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_rule_audit_logs", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_categorization_rules_group_id",
                schema: "ledger",
                table: "categorization_rules",
                column: "group_id");

            migrationBuilder.CreateIndex(
                name: "ix_categorization_rules_is_template",
                schema: "ledger",
                table: "categorization_rules",
                column: "is_template");

            migrationBuilder.CreateIndex(
                name: "ix_rule_audit_logs_evaluated_at",
                schema: "ledger",
                table: "rule_audit_logs",
                column: "evaluated_at");

            migrationBuilder.CreateIndex(
                name: "ix_rule_audit_logs_rule_id",
                schema: "ledger",
                table: "rule_audit_logs",
                column: "rule_id");

            migrationBuilder.CreateIndex(
                name: "ix_rule_audit_logs_transaction_id",
                schema: "ledger",
                table: "rule_audit_logs",
                column: "transaction_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "rule_audit_logs",
                schema: "ledger");

            migrationBuilder.DropIndex(
                name: "ix_categorization_rules_group_id",
                schema: "ledger",
                table: "categorization_rules");

            migrationBuilder.DropIndex(
                name: "ix_categorization_rules_is_template",
                schema: "ledger",
                table: "categorization_rules");

            migrationBuilder.DropColumn(
                name: "group_id",
                schema: "ledger",
                table: "categorization_rules");

            migrationBuilder.DropColumn(
                name: "is_template",
                schema: "ledger",
                table: "categorization_rules");

            migrationBuilder.DropColumn(
                name: "schedule_expression",
                schema: "ledger",
                table: "categorization_rules");

            migrationBuilder.DropColumn(
                name: "tags",
                schema: "ledger",
                table: "categorization_rules");
        }
    }
}
