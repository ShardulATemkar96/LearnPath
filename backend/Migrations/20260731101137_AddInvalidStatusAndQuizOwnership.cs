using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LearnPath.API.Migrations
{
    /// <inheritdoc />
    public partial class AddInvalidStatusAndQuizOwnership : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CreatedById",
                table: "Quizzes",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "InvalidReason",
                table: "AspNetUsers",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.Sql(
                "UPDATE [Quizzes] SET [CreatedById] = (SELECT TOP 1 [Id] FROM [AspNetUsers] WHERE [IsSuperAdmin] = 1) WHERE [CreatedById] IS NULL;");

            migrationBuilder.CreateIndex(
                name: "IX_Quizzes_CreatedById",
                table: "Quizzes",
                column: "CreatedById");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Quizzes_CreatedById",
                table: "Quizzes");

            migrationBuilder.DropColumn(
                name: "CreatedById",
                table: "Quizzes");

            migrationBuilder.DropColumn(
                name: "InvalidReason",
                table: "AspNetUsers");
        }
    }
}
