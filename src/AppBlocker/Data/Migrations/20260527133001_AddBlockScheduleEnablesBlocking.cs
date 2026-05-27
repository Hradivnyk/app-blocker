using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AppBlocker.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddBlockScheduleEnablesBlocking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "EnablesBlocking",
                table: "BlockSchedules",
                type: "INTEGER",
                nullable: false,
                defaultValue: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EnablesBlocking",
                table: "BlockSchedules");
        }
    }
}
