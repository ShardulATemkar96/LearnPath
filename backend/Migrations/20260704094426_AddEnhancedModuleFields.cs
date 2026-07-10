using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LearnPath.API.Migrations
{
    /// <inheritdoc />
    public partial class AddEnhancedModuleFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "ArchivedAt",
                table: "Modules",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Difficulty",
                table: "Modules",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "EstimatedDurationMinutes",
                table: "Modules",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsArchived",
                table: "Modules",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsDraft",
                table: "Modules",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "NotesHtml",
                table: "Modules",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PdfUrl",
                table: "Modules",
                type: "nvarchar(2048)",
                maxLength: 2048,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "QuizEnabled",
                table: "Modules",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "QuizPassingScore",
                table: "Modules",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "QuizQuestionCount",
                table: "Modules",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "QuizTimeLimitMinutes",
                table: "Modules",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ThumbnailUrl",
                table: "Modules",
                type: "nvarchar(2048)",
                maxLength: 2048,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ModuleObjectives",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ModuleId = table.Column<int>(type: "int", nullable: false),
                    ObjectiveText = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    OrderIndex = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ModuleObjectives", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ModuleObjectives_Modules_ModuleId",
                        column: x => x.ModuleId,
                        principalTable: "Modules",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ModuleResources",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ModuleId = table.Column<int>(type: "int", nullable: false),
                    Type = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Url = table.Column<string>(type: "nvarchar(2048)", maxLength: 2048, nullable: false),
                    OrderIndex = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ModuleResources", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ModuleResources_Modules_ModuleId",
                        column: x => x.ModuleId,
                        principalTable: "Modules",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ModuleTags",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ModuleId = table.Column<int>(type: "int", nullable: false),
                    TagName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ModuleTags", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ModuleTags_Modules_ModuleId",
                        column: x => x.ModuleId,
                        principalTable: "Modules",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ModuleObjectives_ModuleId",
                table: "ModuleObjectives",
                column: "ModuleId");

            migrationBuilder.CreateIndex(
                name: "IX_ModuleResources_ModuleId",
                table: "ModuleResources",
                column: "ModuleId");

            migrationBuilder.CreateIndex(
                name: "IX_ModuleTags_ModuleId",
                table: "ModuleTags",
                column: "ModuleId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ModuleObjectives");

            migrationBuilder.DropTable(
                name: "ModuleResources");

            migrationBuilder.DropTable(
                name: "ModuleTags");

            migrationBuilder.DropColumn(
                name: "ArchivedAt",
                table: "Modules");

            migrationBuilder.DropColumn(
                name: "Difficulty",
                table: "Modules");

            migrationBuilder.DropColumn(
                name: "EstimatedDurationMinutes",
                table: "Modules");

            migrationBuilder.DropColumn(
                name: "IsArchived",
                table: "Modules");

            migrationBuilder.DropColumn(
                name: "IsDraft",
                table: "Modules");

            migrationBuilder.DropColumn(
                name: "NotesHtml",
                table: "Modules");

            migrationBuilder.DropColumn(
                name: "PdfUrl",
                table: "Modules");

            migrationBuilder.DropColumn(
                name: "QuizEnabled",
                table: "Modules");

            migrationBuilder.DropColumn(
                name: "QuizPassingScore",
                table: "Modules");

            migrationBuilder.DropColumn(
                name: "QuizQuestionCount",
                table: "Modules");

            migrationBuilder.DropColumn(
                name: "QuizTimeLimitMinutes",
                table: "Modules");

            migrationBuilder.DropColumn(
                name: "ThumbnailUrl",
                table: "Modules");
        }
    }
}
