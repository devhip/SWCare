using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SWCare.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddUserCanViewAllTickets : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "CanViewAllTickets",
                table: "Users",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CanViewAllTickets",
                table: "Users");
        }
    }
}
