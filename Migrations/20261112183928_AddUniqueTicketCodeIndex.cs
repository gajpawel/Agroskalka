using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Agroskalka.Migrations
{
    /// <inheritdoc />
    public partial class AddUniqueTicketCodeIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "ix_tickets_code",
                table: "tickets",
                column: "code",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_tickets_code",
                table: "tickets");
        }
    }
}
