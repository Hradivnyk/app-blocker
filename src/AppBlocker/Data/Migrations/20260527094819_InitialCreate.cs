using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AppBlocker.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BlockedApps",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", maxLength: 260, nullable: false),
                    IsEnabled = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BlockedApps", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "BlockSchedules",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    BlockedAppId = table.Column<int>(type: "INTEGER", nullable: false),
                    CronExpression = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    IsEnabled = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BlockSchedules", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BlockSchedules_BlockedApps_BlockedAppId",
                        column: x => x.BlockedAppId,
                        principalTable: "BlockedApps",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BlockSchedules_BlockedAppId",
                table: "BlockSchedules",
                column: "BlockedAppId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BlockSchedules");

            migrationBuilder.DropTable(
                name: "BlockedApps");
        }
    }
}
