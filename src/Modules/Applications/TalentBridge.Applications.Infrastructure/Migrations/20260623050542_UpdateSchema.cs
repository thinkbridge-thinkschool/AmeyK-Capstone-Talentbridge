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
            migrationBuilder.DropColumn(
                name: "Error",
                table: "ApplicationsOutboxMessages");

            migrationBuilder.DropColumn(
                name: "RetryCount",
                table: "ApplicationsOutboxMessages");

            migrationBuilder.RenameColumn(
                name: "RejectionReason",
                table: "JobApplications",
                newName: "ReviewNotes");

            migrationBuilder.RenameColumn(
                name: "AppliedAt",
                table: "JobApplications",
                newName: "SubmittedAtUtc");

            migrationBuilder.RenameColumn(
                name: "ProcessedAt",
                table: "ApplicationsOutboxMessages",
                newName: "ProcessedOnUtc");

            migrationBuilder.RenameColumn(
                name: "EventType",
                table: "ApplicationsOutboxMessages",
                newName: "Type");

            migrationBuilder.RenameColumn(
                name: "CreatedAt",
                table: "ApplicationsOutboxMessages",
                newName: "OccurredOnUtc");

            migrationBuilder.AddColumn<DateTime>(
                name: "LastUpdatedAtUtc",
                table: "JobApplications",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<Guid>(
                name: "ReviewedByHRId",
                table: "JobApplications",
                type: "uniqueidentifier",
                nullable: true);
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
