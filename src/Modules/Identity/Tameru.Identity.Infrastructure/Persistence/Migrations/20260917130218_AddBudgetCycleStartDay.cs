using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tameru.Identity.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddBudgetCycleStartDay : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "budget_cycle_start_day",
                schema: "identity",
                table: "users",
                type: "integer",
                nullable: false,
                defaultValue: 1);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "budget_cycle_start_day",
                schema: "identity",
                table: "users");
        }
    }
}
