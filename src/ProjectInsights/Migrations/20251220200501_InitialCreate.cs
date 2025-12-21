using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace ProjectInsights.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "analysis_runs",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    github_owner = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    github_repo = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    start_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    end_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    base_branch = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    run_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    pr_count = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_analysis_runs", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "daily_project_stats",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    day = table.Column<DateOnly>(type: "date", nullable: false),
                    project_name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    project_group = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    pr_count = table.Column<int>(type: "integer", nullable: false),
                    total_lines_changed = table.Column<int>(type: "integer", nullable: false),
                    files_modified = table.Column<int>(type: "integer", nullable: false),
                    files_added = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_daily_project_stats", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "daily_team_project_stats",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    day = table.Column<DateOnly>(type: "date", nullable: false),
                    project_name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    project_group = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    team_name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    pr_count = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_daily_team_project_stats", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "pull_requests",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    analysis_run_id = table.Column<long>(type: "bigint", nullable: false),
                    pr_number = table.Column<int>(type: "integer", nullable: false),
                    title = table.Column<string>(type: "text", nullable: false),
                    author = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    team = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    merged_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    merge_commit_sha = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    is_rollup_pr = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_pull_requests", x => x.id);
                    table.ForeignKey(
                        name: "FK_pull_requests_analysis_runs_analysis_run_id",
                        column: x => x.analysis_run_id,
                        principalTable: "analysis_runs",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "pr_files",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    pull_request_id = table.Column<long>(type: "bigint", nullable: false),
                    file_name = table.Column<string>(type: "text", nullable: false),
                    project_name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    project_group = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    additions = table.Column<int>(type: "integer", nullable: false),
                    deletions = table.Column<int>(type: "integer", nullable: false),
                    changes = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_pr_files", x => x.id);
                    table.ForeignKey(
                        name: "FK_pr_files_pull_requests_pull_request_id",
                        column: x => x.pull_request_id,
                        principalTable: "pull_requests",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "pr_projects",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    pull_request_id = table.Column<long>(type: "bigint", nullable: false),
                    project_name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    project_group = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    file_count = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_pr_projects", x => x.id);
                    table.ForeignKey(
                        name: "FK_pr_projects_pull_requests_pull_request_id",
                        column: x => x.pull_request_id,
                        principalTable: "pull_requests",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_daily_project_stats_day",
                table: "daily_project_stats",
                column: "day");

            migrationBuilder.CreateIndex(
                name: "IX_daily_project_stats_day_project_name",
                table: "daily_project_stats",
                columns: new[] { "day", "project_name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_daily_project_stats_project_group",
                table: "daily_project_stats",
                column: "project_group");

            migrationBuilder.CreateIndex(
                name: "IX_daily_team_project_stats_day",
                table: "daily_team_project_stats",
                column: "day");

            migrationBuilder.CreateIndex(
                name: "IX_daily_team_project_stats_day_project_name_team_name",
                table: "daily_team_project_stats",
                columns: new[] { "day", "project_name", "team_name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_daily_team_project_stats_project_group",
                table: "daily_team_project_stats",
                column: "project_group");

            migrationBuilder.CreateIndex(
                name: "IX_daily_team_project_stats_team_name",
                table: "daily_team_project_stats",
                column: "team_name");

            migrationBuilder.CreateIndex(
                name: "IX_pr_files_project_group",
                table: "pr_files",
                column: "project_group");

            migrationBuilder.CreateIndex(
                name: "IX_pr_files_project_name",
                table: "pr_files",
                column: "project_name");

            migrationBuilder.CreateIndex(
                name: "IX_pr_files_pull_request_id",
                table: "pr_files",
                column: "pull_request_id");

            migrationBuilder.CreateIndex(
                name: "IX_pr_projects_pull_request_id_project_name",
                table: "pr_projects",
                columns: new[] { "pull_request_id", "project_name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_pull_requests_analysis_run_id_pr_number",
                table: "pull_requests",
                columns: new[] { "analysis_run_id", "pr_number" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "daily_project_stats");

            migrationBuilder.DropTable(
                name: "daily_team_project_stats");

            migrationBuilder.DropTable(
                name: "pr_files");

            migrationBuilder.DropTable(
                name: "pr_projects");

            migrationBuilder.DropTable(
                name: "pull_requests");

            migrationBuilder.DropTable(
                name: "analysis_runs");
        }
    }
}
