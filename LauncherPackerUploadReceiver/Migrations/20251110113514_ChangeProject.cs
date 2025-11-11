using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LauncherPackerUploadReceiver.Migrations
{
    /// <inheritdoc />
    public partial class ChangeProject : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ProjectAccesses");

            migrationBuilder.AddColumn<string>(
                name: "AccessibleProjectIds",
                table: "Users",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "Projects",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ProjectName = table.Column<string>(type: "varchar(255)", unicode: false, maxLength: 255, nullable: false),
                    TargetPath = table.Column<string>(type: "varchar(500)", unicode: false, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Projects", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Projects");

            migrationBuilder.DropColumn(
                name: "AccessibleProjectIds",
                table: "Users");

            migrationBuilder.CreateTable(
                name: "ProjectAccesses",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    UserId = table.Column<int>(type: "INTEGER", nullable: false),
                    ProjectName = table.Column<string>(type: "TEXT", nullable: false),
                    TargetPath = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProjectAccesses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProjectAccesses_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ProjectAccesses_UserId",
                table: "ProjectAccesses",
                column: "UserId");
        }
    }
}
