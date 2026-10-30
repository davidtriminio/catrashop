using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Catrashop.Modules.Payments.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddManualPayments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "method",
                schema: "payments",
                table: "payments",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "proof_submitted_at_utc",
                schema: "payments",
                table: "payments",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "receipt_content_type",
                schema: "payments",
                table: "payments",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "receipt_key",
                schema: "payments",
                table: "payments",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "review_note",
                schema: "payments",
                table: "payments",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "transfer_bank_account_code",
                schema: "payments",
                table: "payments",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "transfer_reference",
                schema: "payments",
                table: "payments",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_payments_status",
                schema: "payments",
                table: "payments",
                column: "status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_payments_status",
                schema: "payments",
                table: "payments");

            migrationBuilder.DropColumn(
                name: "method",
                schema: "payments",
                table: "payments");

            migrationBuilder.DropColumn(
                name: "proof_submitted_at_utc",
                schema: "payments",
                table: "payments");

            migrationBuilder.DropColumn(
                name: "receipt_content_type",
                schema: "payments",
                table: "payments");

            migrationBuilder.DropColumn(
                name: "receipt_key",
                schema: "payments",
                table: "payments");

            migrationBuilder.DropColumn(
                name: "review_note",
                schema: "payments",
                table: "payments");

            migrationBuilder.DropColumn(
                name: "transfer_bank_account_code",
                schema: "payments",
                table: "payments");

            migrationBuilder.DropColumn(
                name: "transfer_reference",
                schema: "payments",
                table: "payments");
        }
    }
}
