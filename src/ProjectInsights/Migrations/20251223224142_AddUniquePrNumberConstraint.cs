using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProjectInsights.Migrations
{
    /// <inheritdoc />
    public partial class AddUniquePrNumberConstraint : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_pull_requests_analysis_run_id_pr_number",
                table: "pull_requests");

            migrationBuilder.CreateIndex(
                name: "IX_pull_requests_analysis_run_id",
                table: "pull_requests",
                column: "analysis_run_id");

            migrationBuilder.CreateIndex(
                name: "IX_pull_requests_pr_number",
                table: "pull_requests",
                column: "pr_number",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_pull_requests_analysis_run_id",
                table: "pull_requests");

            migrationBuilder.DropIndex(
                name: "IX_pull_requests_pr_number",
                table: "pull_requests");

            migrationBuilder.CreateIndex(
                name: "IX_pull_requests_analysis_run_id_pr_number",
                table: "pull_requests",
                columns: new[] { "analysis_run_id", "pr_number" },
                unique: true);
        }
    }
}
