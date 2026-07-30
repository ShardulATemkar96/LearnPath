using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LearnPath.API.Migrations
{
    /// <inheritdoc />
    public partial class RemoveContentUrlAddFileFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ContentUrl",
                table: "Submissions");

            migrationBuilder.AddColumn<string>(
                name: "FileExtension",
                table: "Submissions",
                type: "nvarchar(10)",
                maxLength: 10,
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "FileSize",
                table: "Submissions",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsLate",
                table: "Submissions",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "MimeType",
                table: "Submissions",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OriginalFileName",
                table: "Submissions",
                type: "nvarchar(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "StoredFilePath",
                table: "Submissions",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FileExtension",
                table: "Submissions");

            migrationBuilder.DropColumn(
                name: "FileSize",
                table: "Submissions");

            migrationBuilder.DropColumn(
                name: "IsLate",
                table: "Submissions");

            migrationBuilder.DropColumn(
                name: "MimeType",
                table: "Submissions");

            migrationBuilder.DropColumn(
                name: "OriginalFileName",
                table: "Submissions");

            migrationBuilder.DropColumn(
                name: "StoredFilePath",
                table: "Submissions");

            migrationBuilder.AddColumn<string>(
                name: "ContentUrl",
                table: "Submissions",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: false,
                defaultValue: "");
        }
    }
}
