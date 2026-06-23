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
            migrationBuilder.DropColumn(
                name: "Error",
                table: "JobsOutboxMessages");

            migrationBuilder.DropColumn(
                name: "RetryCount",
                table: "JobsOutboxMessages");

            migrationBuilder.DropColumn(
                name: "RequiredSkills",
                table: "Jobs");

            migrationBuilder.DropColumn(
                name: "Type",
                table: "Jobs");

            migrationBuilder.RenameColumn(
                name: "ProcessedAt",
                table: "JobsOutboxMessages",
                newName: "ProcessedOnUtc");

            migrationBuilder.RenameColumn(
                name: "EventType",
                table: "JobsOutboxMessages",
                newName: "Type");

            migrationBuilder.RenameColumn(
                name: "CreatedAt",
                table: "JobsOutboxMessages",
                newName: "OccurredOnUtc");

            migrationBuilder.RenameColumn(
                name: "ClosingDate",
                table: "Jobs",
                newName: "PublishedAtUtc");

            migrationBuilder.AddColumn<DateTime>(
                name: "ClosedAtUtc",
                table: "Jobs",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAtUtc",
                table: "Jobs",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "ExpiresAtUtc",
                table: "Jobs",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "PostedByHRId",
                table: "Jobs",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));
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
