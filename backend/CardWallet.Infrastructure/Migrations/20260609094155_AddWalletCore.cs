using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CardWallet.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddWalletCore : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Version",
                table: "wallets");

            migrationBuilder.RenameColumn(
                name: "ReferenceId",
                table: "transactions",
                newName: "ReferenceCode");

            migrationBuilder.AddColumn<string>(
                name: "Currency",
                table: "wallets",
                type: "longtext",
                nullable: false)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<long>(
                name: "LockedBalance",
                table: "wallets",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<long>(
                name: "BalanceAfter",
                table: "transactions",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<long>(
                name: "BalanceBefore",
                table: "transactions",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<string>(
                name: "IdempotencyKey",
                table: "transactions",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<Guid>(
                name: "UserId",
                table: "transactions",
                type: "char(36)",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                collation: "ascii_general_ci");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Currency",
                table: "wallets");

            migrationBuilder.DropColumn(
                name: "LockedBalance",
                table: "wallets");

            migrationBuilder.DropColumn(
                name: "BalanceAfter",
                table: "transactions");

            migrationBuilder.DropColumn(
                name: "BalanceBefore",
                table: "transactions");

            migrationBuilder.DropColumn(
                name: "IdempotencyKey",
                table: "transactions");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "transactions");

            migrationBuilder.RenameColumn(
                name: "ReferenceCode",
                table: "transactions",
                newName: "ReferenceId");

            migrationBuilder.AddColumn<Guid>(
                name: "Version",
                table: "wallets",
                type: "char(36)",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                collation: "ascii_general_ci");
        }
    }
}
