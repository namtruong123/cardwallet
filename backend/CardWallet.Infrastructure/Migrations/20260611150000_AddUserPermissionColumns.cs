using System;
using CardWallet.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CardWallet.Infrastructure.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(AppDbContext))]
    [Migration("20260611150000_AddUserPermissionColumns")]
    public partial class AddUserPermissionColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Role",
                table: "users",
                type: "varchar(40)",
                maxLength: 40,
                nullable: false,
                defaultValue: "Customer");

            migrationBuilder.AddColumn<Guid>(
                name: "ParentUserId",
                table: "users",
                type: "char(36)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "CanManageUsers",
                table: "users",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "CanManageTasks",
                table: "users",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "CanApproveTasks",
                table: "users",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "CanApproveKycWithdraw",
                table: "users",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "CanTransferPoints",
                table: "users",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "CanManageBlog",
                table: "users",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "CanExportReports",
                table: "users",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateIndex(
                name: "IX_users_ParentUserId",
                table: "users",
                column: "ParentUserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_users_ParentUserId",
                table: "users");

            migrationBuilder.DropColumn(
                name: "Role",
                table: "users");

            migrationBuilder.DropColumn(
                name: "ParentUserId",
                table: "users");

            migrationBuilder.DropColumn(
                name: "CanManageUsers",
                table: "users");

            migrationBuilder.DropColumn(
                name: "CanManageTasks",
                table: "users");

            migrationBuilder.DropColumn(
                name: "CanApproveTasks",
                table: "users");

            migrationBuilder.DropColumn(
                name: "CanApproveKycWithdraw",
                table: "users");

            migrationBuilder.DropColumn(
                name: "CanTransferPoints",
                table: "users");

            migrationBuilder.DropColumn(
                name: "CanManageBlog",
                table: "users");

            migrationBuilder.DropColumn(
                name: "CanExportReports",
                table: "users");
        }
    }
}
