using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Liaro.Migrations
{
    public partial class addmobilelogin : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "LoginCode",
                table: "Users",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "MobileLoginExpire",
                table: "Users",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LoginCode",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "MobileLoginExpire",
                table: "Users");
        }
    }
}
