using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LearnPath.API.Migrations
{
    /// <inheritdoc />
    public partial class AddUserStatusAndSuperAdmin : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Status",
                table: "AspNetUsers",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.Sql(
                "UPDATE AspNetUsers SET Status = CASE WHEN IsActive = 1 THEN 0 ELSE 1 END;");

            migrationBuilder.RenameColumn(
                name: "IsActive",
                table: "AspNetUsers",
                newName: "IsSuperAdmin");

            migrationBuilder.Sql(
                "UPDATE AspNetUsers SET IsSuperAdmin = 0;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                "UPDATE AspNetUsers SET IsSuperAdmin = CASE WHEN Status = 0 THEN 1 ELSE 0 END;");

            migrationBuilder.RenameColumn(
                name: "IsSuperAdmin",
                table: "AspNetUsers",
                newName: "IsActive");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "AspNetUsers");
        }
    }
}
