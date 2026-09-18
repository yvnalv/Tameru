using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tameru.Ledger.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddAdvancedCategorizationRules : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "account_id",
                schema: "ledger",
                table: "categorization_rules",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "max_amount",
                schema: "ledger",
                table: "categorization_rules",
                type: "numeric(19,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "min_amount",
                schema: "ledger",
                table: "categorization_rules",
                type: "numeric(19,2)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "replace_title",
                schema: "ledger",
                table: "categorization_rules",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "transaction_type",
                schema: "ledger",
                table: "categorization_rules",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "account_id",
                schema: "ledger",
                table: "categorization_rules");

            migrationBuilder.DropColumn(
                name: "max_amount",
                schema: "ledger",
                table: "categorization_rules");

            migrationBuilder.DropColumn(
                name: "min_amount",
                schema: "ledger",
                table: "categorization_rules");

            migrationBuilder.DropColumn(
                name: "replace_title",
                schema: "ledger",
                table: "categorization_rules");

            migrationBuilder.DropColumn(
                name: "transaction_type",
                schema: "ledger",
                table: "categorization_rules");
        }
    }
}
