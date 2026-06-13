using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CardWallet.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCardTransactionFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ErrorMessage",
                table: "card_transactions",
                type: "varchar(1000)",
                maxLength: 1000,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<DateTime>(
                name: "NextRetryAt",
                table: "card_transactions",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ParentRequestRaw",
                table: "card_transactions",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "ParentResponseRaw",
                table: "card_transactions",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<DateTime>(
                name: "ProcessedAt",
                table: "card_transactions",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "RetryCount",
                table: "card_transactions",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ErrorMessage",
                table: "card_transactions");

            migrationBuilder.DropColumn(
                name: "NextRetryAt",
                table: "card_transactions");

            migrationBuilder.DropColumn(
                name: "ParentRequestRaw",
                table: "card_transactions");

            migrationBuilder.DropColumn(
                name: "ParentResponseRaw",
                table: "card_transactions");

            migrationBuilder.DropColumn(
                name: "ProcessedAt",
                table: "card_transactions");

            migrationBuilder.DropColumn(
                name: "RetryCount",
                table: "card_transactions");
        }
    }
}
