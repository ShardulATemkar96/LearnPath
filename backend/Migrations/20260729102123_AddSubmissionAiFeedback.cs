using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LearnPath.API.Migrations
{
    /// <inheritdoc />
    public partial class AddSubmissionAiFeedback : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SubmissionAiFeedbacks",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SubmissionId = table.Column<int>(type: "int", nullable: false),
                    Summary = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    GrammarFeedback = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    RubricCoverage = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    MissingTopics = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    SuggestedScorePercentage = table.Column<int>(type: "int", nullable: false),
                    SuggestedScoreMarks = table.Column<int>(type: "int", nullable: false),
                    OverallRecommendation = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    Disclaimer = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    RawResponse = table.Column<string>(type: "nvarchar(max)", maxLength: 16000, nullable: false),
                    GeneratedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SubmissionAiFeedbacks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SubmissionAiFeedbacks_Submissions_SubmissionId",
                        column: x => x.SubmissionId,
                        principalTable: "Submissions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SubmissionAiFeedbacks_SubmissionId",
                table: "SubmissionAiFeedbacks",
                column: "SubmissionId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SubmissionAiFeedbacks");
        }
    }
}
