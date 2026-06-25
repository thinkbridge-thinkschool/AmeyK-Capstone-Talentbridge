using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TalentBridge.Identity.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class SyncModelSnapshot : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                IF COL_LENGTH('[Users]', 'CreatedAt') IS NULL
                    ALTER TABLE [Users] ADD [CreatedAt] datetime2 NOT NULL DEFAULT '0001-01-01T00:00:00.0000000';");
            migrationBuilder.Sql(@"
                IF COL_LENGTH('[Users]', 'UpdatedAt') IS NULL
                    ALTER TABLE [Users] ADD [UpdatedAt] datetime2 NULL;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "Users");
        }
    }
}
