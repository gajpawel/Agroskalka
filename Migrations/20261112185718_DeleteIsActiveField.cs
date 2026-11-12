using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Agroskalka.Migrations
{
    /// <inheritdoc />
    public partial class DeleteIsActiveField : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "is_active",
                table: "tickets");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "is_active",
                table: "tickets",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }
    }
}
