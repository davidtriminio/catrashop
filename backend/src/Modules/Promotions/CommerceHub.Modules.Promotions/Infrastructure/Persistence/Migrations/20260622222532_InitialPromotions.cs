using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CommerceHub.Modules.Promotions.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialPromotions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "promotions");

            migrationBuilder.CreateTable(
                name: "coupons",
                schema: "promotions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    discount_type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    discount_value = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: true),
                    minimum_subtotal = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    max_discount_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    starts_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ends_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    usage_limit = table.Column<int>(type: "integer", nullable: true),
                    per_customer_limit = table.Column<int>(type: "integer", nullable: true),
                    times_redeemed = table.Column<int>(type: "integer", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_coupons", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "outbox_messages",
                schema: "promotions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    type = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    content = table.Column<string>(type: "jsonb", nullable: false),
                    occurred_on_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    processed_on_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    error = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    retry_count = table.Column<int>(type: "integer", nullable: false),
                    correlation_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_outbox_messages", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "coupon_redemptions",
                schema: "promotions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    coupon_id = table.Column<Guid>(type: "uuid", nullable: false),
                    customer_id = table.Column<Guid>(type: "uuid", nullable: false),
                    reference = table.Column<Guid>(type: "uuid", nullable: false),
                    coupon_code = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    discount_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    redeemed_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    released_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_coupon_redemptions", x => x.id);
                    table.ForeignKey(
                        name: "fk_coupon_redemptions_coupons_coupon_id",
                        column: x => x.coupon_id,
                        principalSchema: "promotions",
                        principalTable: "coupons",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_coupon_redemptions_coupon_customer",
                schema: "promotions",
                table: "coupon_redemptions",
                columns: new[] { "coupon_id", "customer_id" });

            migrationBuilder.CreateIndex(
                name: "ux_coupon_redemptions_reference",
                schema: "promotions",
                table: "coupon_redemptions",
                column: "reference",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_coupons_code",
                schema: "promotions",
                table: "coupons",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_outbox_messages_unprocessed",
                schema: "promotions",
                table: "outbox_messages",
                columns: new[] { "processed_on_utc", "occurred_on_utc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "coupon_redemptions",
                schema: "promotions");

            migrationBuilder.DropTable(
                name: "outbox_messages",
                schema: "promotions");

            migrationBuilder.DropTable(
                name: "coupons",
                schema: "promotions");
        }
    }
}
