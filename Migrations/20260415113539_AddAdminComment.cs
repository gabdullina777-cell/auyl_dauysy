using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ayul_dayusy.Migrations
{
    /// <inheritdoc />
    public partial class AddAdminComment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AdminComment",
                table: "Petitions",
                type: "TEXT",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AdminComment",
                table: "Petitions");
        }
    }
}
