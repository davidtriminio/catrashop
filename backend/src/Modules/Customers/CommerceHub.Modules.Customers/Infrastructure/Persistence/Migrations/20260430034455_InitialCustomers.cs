using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CommerceHub.Modules.Customers.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCustomers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "customers");

            migrationBuilder.CreateTable(
                name: "customers",
                schema: "customers",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    email = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    first_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    last_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    phone = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    marketing_emails_opt_in = table.Column<bool>(type: "boolean", nullable: false),
                    preferred_language = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_customers", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "outbox_messages",
                schema: "customers",
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
                name: "addresses",
                schema: "customers",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    customer_id = table.Column<Guid>(type: "uuid", nullable: false),
                    label = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    recipient_name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    line1 = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    line2 = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    city = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    region = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    postal_code = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    country = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: false),
                    phone = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    is_default_shipping = table.Column<bool>(type: "boolean", nullable: false),
                    is_default_billing = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_addresses", x => x.id);
                    table.ForeignKey(
                        name: "fk_addresses_customers_customer_id",
                        column: x => x.customer_id,
                        principalSchema: "customers",
                        principalTable: "customers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "wishlist_items",
                schema: "customers",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    customer_id = table.Column<Guid>(type: "uuid", nullable: false),
                    product_id = table.Column<Guid>(type: "uuid", nullable: false),
                    added_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_wishlist_items", x => x.id);
                    table.ForeignKey(
                        name: "fk_wishlist_items_customers_customer_id",
                        column: x => x.customer_id,
                        principalSchema: "customers",
                        principalTable: "customers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_addresses_customer_id",
                schema: "customers",
                table: "addresses",
                column: "customer_id");

            migrationBuilder.CreateIndex(
                name: "ix_customers_email",
                schema: "customers",
                table: "customers",
                column: "email");

            migrationBuilder.CreateIndex(
                name: "ix_outbox_messages_unprocessed",
                schema: "customers",
                table: "outbox_messages",
                columns: new[] { "processed_on_utc", "occurred_on_utc" });

            migrationBuilder.CreateIndex(
                name: "ix_wishlist_customer_product",
                schema: "customers",
                table: "wishlist_items",
                columns: new[] { "customer_id", "product_id" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "addresses",
                schema: "customers");

            migrationBuilder.DropTable(
                name: "outbox_messages",
                schema: "customers");

            migrationBuilder.DropTable(
                name: "wishlist_items",
                schema: "customers");

            migrationBuilder.DropTable(
                name: "customers",
                schema: "customers");
        }
    }
}
