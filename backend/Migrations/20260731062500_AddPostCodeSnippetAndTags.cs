using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LearnPath.API.Migrations
{
    /// <inheritdoc />
    public partial class AddPostCodeSnippetAndTags : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CodeSnippet",
                table: "Posts",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ProgrammingLanguage",
                table: "Posts",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Tags",
                table: "Posts",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CodeSnippet",
                table: "Posts");

            migrationBuilder.DropColumn(
                name: "ProgrammingLanguage",
                table: "Posts");

            migrationBuilder.DropColumn(
                name: "Tags",
                table: "Posts");
        }
    }
}
