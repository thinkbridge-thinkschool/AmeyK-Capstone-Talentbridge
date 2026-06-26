using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TalentBridge.Applications.Infrastructure.Migrations
{
    public partial class EnsureStatusHistoryTable : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Idempotent — creates table only if it doesn't already exist.
            migrationBuilder.Sql("""
                IF NOT EXISTS (SELECT 1 FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[ApplicationStatusHistory]') AND type = 'U')
                BEGIN
                    CREATE TABLE [dbo].[ApplicationStatusHistory] (
                        [Id]              uniqueidentifier  NOT NULL,
                        [ApplicationId]   uniqueidentifier  NOT NULL,
                        [FromStatus]      nvarchar(50)      NOT NULL,
                        [ToStatus]        nvarchar(50)      NOT NULL,
                        [ChangedByUserId] uniqueidentifier  NULL,
                        [Notes]           nvarchar(1000)    NULL,
                        [ChangedAtUtc]    datetime2         NOT NULL,
                        CONSTRAINT [PK_ApplicationStatusHistory] PRIMARY KEY ([Id])
                    );
                    CREATE INDEX [IX_ApplicationStatusHistory_ApplicationId]
                        ON [dbo].[ApplicationStatusHistory] ([ApplicationId]);
                END
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                IF EXISTS (SELECT 1 FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[ApplicationStatusHistory]') AND type = 'U')
                    DROP TABLE [dbo].[ApplicationStatusHistory];
                """);
        }
    }
}
