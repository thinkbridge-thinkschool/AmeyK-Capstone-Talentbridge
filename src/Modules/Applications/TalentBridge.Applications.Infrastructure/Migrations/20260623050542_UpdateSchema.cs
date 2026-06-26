using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TalentBridge.Applications.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UpdateSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Idempotent drops
            migrationBuilder.Sql("IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[ApplicationsOutboxMessages]') AND name = 'Error') ALTER TABLE [ApplicationsOutboxMessages] DROP COLUMN [Error];");
            migrationBuilder.Sql("IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[ApplicationsOutboxMessages]') AND name = 'RetryCount') ALTER TABLE [ApplicationsOutboxMessages] DROP COLUMN [RetryCount];");

            // Idempotent renames — only rename if old column still exists
            migrationBuilder.Sql("IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[JobApplications]') AND name = 'RejectionReason') EXEC sp_rename '[JobApplications].[RejectionReason]', 'ReviewNotes', 'COLUMN';");
            migrationBuilder.Sql("IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[JobApplications]') AND name = 'AppliedAt') EXEC sp_rename '[JobApplications].[AppliedAt]', 'SubmittedAtUtc', 'COLUMN';");
            migrationBuilder.Sql("IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[ApplicationsOutboxMessages]') AND name = 'ProcessedAt') EXEC sp_rename '[ApplicationsOutboxMessages].[ProcessedAt]', 'ProcessedOnUtc', 'COLUMN';");
            migrationBuilder.Sql("IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[ApplicationsOutboxMessages]') AND name = 'EventType') EXEC sp_rename '[ApplicationsOutboxMessages].[EventType]', 'Type', 'COLUMN';");
            migrationBuilder.Sql("IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[ApplicationsOutboxMessages]') AND name = 'CreatedAt') EXEC sp_rename '[ApplicationsOutboxMessages].[CreatedAt]', 'OccurredOnUtc', 'COLUMN';");

            // Idempotent adds
            migrationBuilder.Sql("IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[JobApplications]') AND name = 'LastUpdatedAtUtc') ALTER TABLE [JobApplications] ADD [LastUpdatedAtUtc] datetime2 NOT NULL DEFAULT '0001-01-01T00:00:00.000';");
            migrationBuilder.Sql("IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[JobApplications]') AND name = 'ReviewedByHRId') ALTER TABLE [JobApplications] ADD [ReviewedByHRId] uniqueidentifier NULL;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LastUpdatedAtUtc",
                table: "JobApplications");

            migrationBuilder.DropColumn(
                name: "ReviewedByHRId",
                table: "JobApplications");

            migrationBuilder.RenameColumn(
                name: "SubmittedAtUtc",
                table: "JobApplications",
                newName: "AppliedAt");

            migrationBuilder.RenameColumn(
                name: "ReviewNotes",
                table: "JobApplications",
                newName: "RejectionReason");

            migrationBuilder.RenameColumn(
                name: "Type",
                table: "ApplicationsOutboxMessages",
                newName: "EventType");

            migrationBuilder.RenameColumn(
                name: "ProcessedOnUtc",
                table: "ApplicationsOutboxMessages",
                newName: "ProcessedAt");

            migrationBuilder.RenameColumn(
                name: "OccurredOnUtc",
                table: "ApplicationsOutboxMessages",
                newName: "CreatedAt");

            migrationBuilder.AddColumn<string>(
                name: "Error",
                table: "ApplicationsOutboxMessages",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "RetryCount",
                table: "ApplicationsOutboxMessages",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }
    }
}
