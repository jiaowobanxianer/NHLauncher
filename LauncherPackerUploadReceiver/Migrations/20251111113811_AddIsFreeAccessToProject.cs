using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LauncherPackerUploadReceiver.Migrations
{
    /// <inheritdoc />
    public partial class AddIsFreeAccessToProject : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsFreeAccess",
                table: "Projects",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsFreeAccess",
                table: "Projects");
        }
    }
}
