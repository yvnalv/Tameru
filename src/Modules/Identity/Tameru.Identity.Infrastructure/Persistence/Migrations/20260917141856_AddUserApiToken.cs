using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tameru.Identity.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddUserApiToken : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "api_token",
                schema: "identity",
                table: "users",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_users_api_token",
                schema: "identity",
                table: "users",
                column: "api_token",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_users_api_token",
                schema: "identity",
                table: "users");

            migrationBuilder.DropColumn(
                name: "api_token",
                schema: "identity",
                table: "users");
        }
    }
}
