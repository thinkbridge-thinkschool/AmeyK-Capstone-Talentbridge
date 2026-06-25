using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TalentBridge.Identity.Infrastructure.Migrations
{
    public partial class AddUserProfileExtended : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Bio",
                table: "Users",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "GitHubUrl",
                table: "Users",
                type: "nvarchar(300)",
                maxLength: 300,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LinkedInUrl",
                table: "Users",
                type: "nvarchar(300)",
                maxLength: 300,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Phone",
                table: "Users",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ResumeUrl",
                table: "Users",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Skills",
                table: "Users",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Title",
                table: "Users",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "Bio", table: "Users");
            migrationBuilder.DropColumn(name: "GitHubUrl", table: "Users");
            migrationBuilder.DropColumn(name: "LinkedInUrl", table: "Users");
            migrationBuilder.DropColumn(name: "Phone", table: "Users");
            migrationBuilder.DropColumn(name: "ResumeUrl", table: "Users");
            migrationBuilder.DropColumn(name: "Skills", table: "Users");
            migrationBuilder.DropColumn(name: "Title", table: "Users");
        }
    }
}
