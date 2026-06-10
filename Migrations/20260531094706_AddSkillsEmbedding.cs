using Microsoft.EntityFrameworkCore.Migrations;
using Pgvector;

#nullable disable

namespace CVAnalyzer.Migrations
{
    /// <inheritdoc />
    public partial class AddSkillsEmbedding : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<Vector>(
                name: "Embedding",
                table: "CandidateCVs",
                type: "vector(3072)",
                nullable: true,
                oldClrType: typeof(Vector),
                oldType: "vector(768)",
                oldNullable: true);

            migrationBuilder.AddColumn<Vector>(
                name: "SkillsEmbedding",
                table: "CandidateCVs",
                type: "vector(3072)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SkillsEmbedding",
                table: "CandidateCVs");

            migrationBuilder.AlterColumn<Vector>(
                name: "Embedding",
                table: "CandidateCVs",
                type: "vector(768)",
                nullable: true,
                oldClrType: typeof(Vector),
                oldType: "vector(3072)",
                oldNullable: true);
        }
    }
}
