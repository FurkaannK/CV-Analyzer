using System;
using System.Collections.Generic;
using CVAnalyzer.Models;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace CVAnalyzer.Migrations
{
    /// <inheritdoc />
    public partial class RemoveCVAnalysisAddCandidateCV : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CVAnalyses");

            migrationBuilder.CreateTable(
                name: "CandidateCVs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<int>(type: "integer", nullable: true),
                    RawText = table.Column<string>(type: "text", nullable: false),
                    ParsedData = table.Column<ATSParsedData>(type: "jsonb", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CandidateCVs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CandidateCVs_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_CandidateCVs_UserId",
                table: "CandidateCVs",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CandidateCVs");

            migrationBuilder.CreateTable(
                name: "CVAnalyses",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<int>(type: "integer", nullable: true),
                    CVText = table.Column<string>(type: "text", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    FoundSkills = table.Column<List<string>>(type: "jsonb", nullable: false),
                    JobMatchScore = table.Column<int>(type: "integer", nullable: false),
                    MatchCriteria = table.Column<List<MatchCriterion>>(type: "jsonb", nullable: false),
                    MissingSkills = table.Column<List<string>>(type: "jsonb", nullable: false),
                    OverallScore = table.Column<int>(type: "integer", nullable: false),
                    ScoreBadge = table.Column<string>(type: "text", nullable: false),
                    Sections = table.Column<Dictionary<string, bool>>(type: "jsonb", nullable: false),
                    SkillLevels = table.Column<Dictionary<string, int>>(type: "jsonb", nullable: false),
                    Suggestions = table.Column<List<SuggestionItem>>(type: "jsonb", nullable: false),
                    TargetRole = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CVAnalyses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CVAnalyses_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_CVAnalyses_UserId",
                table: "CVAnalyses",
                column: "UserId");
        }
    }
}
