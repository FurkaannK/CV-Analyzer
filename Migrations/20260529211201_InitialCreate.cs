using System;
using System.Collections.Generic;
using CVAnalyzer.Models;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace CVAnalyzer.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "JobRoles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    RoleName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    RequiredSkills = table.Column<string>(type: "text", nullable: false),
                    IconClass = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_JobRoles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    FullName = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    Email = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CVAnalyses",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<int>(type: "integer", nullable: true),
                    CVText = table.Column<string>(type: "text", nullable: false),
                    TargetRole = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    OverallScore = table.Column<int>(type: "integer", nullable: false),
                    JobMatchScore = table.Column<int>(type: "integer", nullable: false),
                    ScoreBadge = table.Column<string>(type: "text", nullable: false),
                    FoundSkills = table.Column<List<string>>(type: "jsonb", nullable: false),
                    MissingSkills = table.Column<List<string>>(type: "jsonb", nullable: false),
                    Suggestions = table.Column<List<SuggestionItem>>(type: "jsonb", nullable: false),
                    Sections = table.Column<Dictionary<string, bool>>(type: "jsonb", nullable: false),
                    MatchCriteria = table.Column<List<MatchCriterion>>(type: "jsonb", nullable: false),
                    SkillLevels = table.Column<Dictionary<string, int>>(type: "jsonb", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
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

            migrationBuilder.InsertData(
                table: "JobRoles",
                columns: new[] { "Id", "Description", "IconClass", "IsActive", "RequiredSkills", "RoleName" },
                values: new object[,]
                {
                    { 1, "Full-stack, frontend, backend", "ti-code", true, "C#,ASP.NET,SQL,JavaScript,HTML,CSS,Git,API,React,Docker", "Software Developer" },
                    { 2, "Dijital, içerik, sosyal medya", "ti-speakerphone", true, "SEO,Google Ads,Analytics,Content Marketing,Social Media,Email Marketing,Canva,CRM", "Marketing Specialist" },
                    { 3, "B2B, müşteri yönetimi", "ti-chart-line", true, "CRM,Negotiation,B2B,Pipeline,Salesforce,KPI,Forecasting,Cold Calling", "Sales Manager" },
                    { 4, "SQL, Python, BI araçları", "ti-chart-bar", true, "SQL,Python,Power BI,Tableau,Excel,Statistics,Machine Learning,ETL,R", "Data Analyst" },
                    { 5, "Figma, kullanıcı araştırması", "ti-palette", true, "Figma,Adobe XD,User Research,Wireframing,Prototyping,CSS,Usability Testing,Design System", "UI/UX Designer" },
                    { 6, "Agile, Scrum, liderlik", "ti-layout-kanban", true, "Agile,Scrum,Jira,Risk Management,Budgeting,Stakeholder Management,PMP,Leadership", "Project Manager" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_CVAnalyses_UserId",
                table: "CVAnalyses",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CVAnalyses");

            migrationBuilder.DropTable(
                name: "JobRoles");

            migrationBuilder.DropTable(
                name: "Users");
        }
    }
}
