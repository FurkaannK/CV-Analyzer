using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CVAnalyzer.Migrations
{
    /// <inheritdoc />
    public partial class AddPdfFilePathToCV : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PdfFilePath",
                table: "CandidateCVs",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PdfFilePath",
                table: "CandidateCVs");
        }
    }
}
