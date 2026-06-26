using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TalentBridge.Jobs.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UpdateSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Idempotent drops
            migrationBuilder.Sql("IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[JobsOutboxMessages]') AND name = 'Error') ALTER TABLE [JobsOutboxMessages] DROP COLUMN [Error];");
            migrationBuilder.Sql("IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[JobsOutboxMessages]') AND name = 'RetryCount') ALTER TABLE [JobsOutboxMessages] DROP COLUMN [RetryCount];");
            migrationBuilder.Sql("IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[Jobs]') AND name = 'RequiredSkills') ALTER TABLE [Jobs] DROP COLUMN [RequiredSkills];");
            migrationBuilder.Sql("IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[Jobs]') AND name = 'Type') ALTER TABLE [Jobs] DROP COLUMN [Type];");

            // Idempotent renames — only rename if old column still exists
            migrationBuilder.Sql("IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[JobsOutboxMessages]') AND name = 'ProcessedAt') EXEC sp_rename '[JobsOutboxMessages].[ProcessedAt]', 'ProcessedOnUtc', 'COLUMN';");
            migrationBuilder.Sql("IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[JobsOutboxMessages]') AND name = 'EventType') EXEC sp_rename '[JobsOutboxMessages].[EventType]', 'Type', 'COLUMN';");
            migrationBuilder.Sql("IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[JobsOutboxMessages]') AND name = 'CreatedAt') EXEC sp_rename '[JobsOutboxMessages].[CreatedAt]', 'OccurredOnUtc', 'COLUMN';");
            migrationBuilder.Sql("IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[Jobs]') AND name = 'ClosingDate') EXEC sp_rename '[Jobs].[ClosingDate]', 'PublishedAtUtc', 'COLUMN';");

            // Idempotent adds
            migrationBuilder.Sql("IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[Jobs]') AND name = 'ClosedAtUtc') ALTER TABLE [Jobs] ADD [ClosedAtUtc] datetime2 NULL;");
            migrationBuilder.Sql("IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[Jobs]') AND name = 'CreatedAtUtc') ALTER TABLE [Jobs] ADD [CreatedAtUtc] datetime2 NOT NULL DEFAULT '0001-01-01T00:00:00.000';");
            migrationBuilder.Sql("IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[Jobs]') AND name = 'ExpiresAtUtc') ALTER TABLE [Jobs] ADD [ExpiresAtUtc] datetime2 NULL;");
            migrationBuilder.Sql("IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[Jobs]') AND name = 'PostedByHRId') ALTER TABLE [Jobs] ADD [PostedByHRId] uniqueidentifier NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000';");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ClosedAtUtc",
                table: "Jobs");

            migrationBuilder.DropColumn(
                name: "CreatedAtUtc",
                table: "Jobs");

            migrationBuilder.DropColumn(
                name: "ExpiresAtUtc",
                table: "Jobs");

            migrationBuilder.DropColumn(
                name: "PostedByHRId",
                table: "Jobs");

            migrationBuilder.RenameColumn(
                name: "Type",
                table: "JobsOutboxMessages",
                newName: "EventType");

            migrationBuilder.RenameColumn(
                name: "ProcessedOnUtc",
                table: "JobsOutboxMessages",
                newName: "ProcessedAt");

            migrationBuilder.RenameColumn(
                name: "OccurredOnUtc",
                table: "JobsOutboxMessages",
                newName: "CreatedAt");

            migrationBuilder.RenameColumn(
                name: "PublishedAtUtc",
                table: "Jobs",
                newName: "ClosingDate");

            migrationBuilder.AddColumn<string>(
                name: "Error",
                table: "JobsOutboxMessages",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "RetryCount",
                table: "JobsOutboxMessages",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "RequiredSkills",
                table: "Jobs",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Type",
                table: "Jobs",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }
    }
}
