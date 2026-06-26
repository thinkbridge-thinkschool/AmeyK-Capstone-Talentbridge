using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TalentBridge.Applications.Infrastructure.Migrations
{
    public partial class EnsureMatchPercentage : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Idempotent — adds column only if it doesn't already exist.
            // Required because the prior AddMatchPercentage migration may have been
            // recorded in history without the DDL actually executing.
            migrationBuilder.Sql("""
                IF NOT EXISTS (
                    SELECT 1 FROM sys.columns
                    WHERE object_id = OBJECT_ID(N'[dbo].[JobApplications]')
                      AND name = 'MatchPercentage'
                )
                ALTER TABLE [dbo].[JobApplications] ADD [MatchPercentage] decimal(5,2) NULL;
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                IF EXISTS (
                    SELECT 1 FROM sys.columns
                    WHERE object_id = OBJECT_ID(N'[dbo].[JobApplications]')
                      AND name = 'MatchPercentage'
                )
                ALTER TABLE [dbo].[JobApplications] DROP COLUMN [MatchPercentage];
                """);
        }
    }
}
