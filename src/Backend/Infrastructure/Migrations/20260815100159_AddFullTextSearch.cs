using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IAMS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddFullTextSearch : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Findings_Title_Category_Description",
                table: "Findings",
                columns: new[] { "Title", "Category", "Description" })
                .Annotation("Npgsql:IndexMethod", "GIN")
                .Annotation("Npgsql:TsVectorConfig", "simple");

            migrationBuilder.CreateIndex(
                name: "IX_CorrectiveActions_Action_PicName_RejectionReason_Verificati~",
                table: "CorrectiveActions",
                columns: new[] { "Action", "PicName", "RejectionReason", "VerificationNote" })
                .Annotation("Npgsql:IndexMethod", "GIN")
                .Annotation("Npgsql:TsVectorConfig", "simple");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Findings_Title_Category_Description",
                table: "Findings");

            migrationBuilder.DropIndex(
                name: "IX_CorrectiveActions_Action_PicName_RejectionReason_Verificati~",
                table: "CorrectiveActions");
        }
    }
}
