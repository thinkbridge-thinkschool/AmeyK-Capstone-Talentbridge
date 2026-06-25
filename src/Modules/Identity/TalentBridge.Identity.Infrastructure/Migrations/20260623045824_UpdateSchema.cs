using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TalentBridge.Identity.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UpdateSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Drop legacy columns only if they exist (migration was applied against different DB state)
            migrationBuilder.Sql("IF COL_LENGTH('[Users]', 'CompanyId') IS NOT NULL ALTER TABLE [Users] DROP COLUMN [CompanyId];");
            migrationBuilder.Sql("IF COL_LENGTH('[Users]', 'FirstName') IS NOT NULL ALTER TABLE [Users] DROP COLUMN [FirstName];");
            migrationBuilder.Sql("IF COL_LENGTH('[Users]', 'IsActive') IS NOT NULL ALTER TABLE [Users] DROP COLUMN [IsActive];");
            migrationBuilder.Sql("IF COL_LENGTH('[Users]', 'LastName') IS NOT NULL ALTER TABLE [Users] DROP COLUMN [LastName];");

            // Rename LastLoginAt → RefreshTokenExpiresAtUtc only if the old column still exists
            migrationBuilder.Sql(@"
                IF COL_LENGTH('[Users]', 'LastLoginAt') IS NOT NULL AND COL_LENGTH('[Users]', 'RefreshTokenExpiresAtUtc') IS NULL
                    EXEC sp_rename '[Users].[LastLoginAt]', 'RefreshTokenExpiresAtUtc', 'COLUMN';");

            // Add new columns only if they don't exist
            migrationBuilder.Sql(@"
                IF COL_LENGTH('[Users]', 'CreatedAtUtc') IS NULL
                    ALTER TABLE [Users] ADD [CreatedAtUtc] datetime2 NOT NULL DEFAULT '0001-01-01T00:00:00.000';");
            migrationBuilder.Sql(@"
                IF COL_LENGTH('[Users]', 'RefreshToken') IS NULL
                    ALTER TABLE [Users] ADD [RefreshToken] nvarchar(max) NULL;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CreatedAtUtc",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "RefreshToken",
                table: "Users");

            migrationBuilder.RenameColumn(
                name: "RefreshTokenExpiresAtUtc",
                table: "Users",
                newName: "LastLoginAt");

            migrationBuilder.AddColumn<Guid>(
                name: "CompanyId",
                table: "Users",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FirstName",
                table: "Users",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "Users",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "LastName",
                table: "Users",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");
        }
    }
}
