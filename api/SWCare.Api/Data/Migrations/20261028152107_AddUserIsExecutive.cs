using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SWCare.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddUserIsExecutive : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsExecutive",
                table: "Users",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsExecutive",
                table: "Users");
        }
    }
}
