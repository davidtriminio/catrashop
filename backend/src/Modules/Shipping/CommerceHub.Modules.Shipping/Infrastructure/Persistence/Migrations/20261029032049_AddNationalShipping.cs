using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CommerceHub.Modules.Shipping.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddNationalShipping : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "pickup_stores",
                schema: "shipping",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    line1 = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    city = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    department_code = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    phone = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    hours = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_pickup_stores", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "shipping_zones",
                schema: "shipping",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    cost = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    estimated_days_min = table.Column<int>(type: "integer", nullable: false),
                    estimated_days_max = table.Column<int>(type: "integer", nullable: false),
                    cash_on_delivery_covered = table.Column<bool>(type: "boolean", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_shipping_zones", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "shipping_zone_departments",
                schema: "shipping",
                columns: table => new
                {
                    zone_id = table.Column<Guid>(type: "uuid", nullable: false),
                    department_code = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_shipping_zone_departments", x => new { x.zone_id, x.department_code });
                    table.ForeignKey(
                        name: "fk_shipping_zone_departments_shipping_zones_zone_id",
                        column: x => x.zone_id,
                        principalSchema: "shipping",
                        principalTable: "shipping_zones",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_pickup_stores_active",
                schema: "shipping",
                table: "pickup_stores",
                column: "is_active");

            migrationBuilder.CreateIndex(
                name: "ux_shipping_zone_departments_department",
                schema: "shipping",
                table: "shipping_zone_departments",
                column: "department_code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_shipping_zones_code",
                schema: "shipping",
                table: "shipping_zones",
                column: "code",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "pickup_stores",
                schema: "shipping");

            migrationBuilder.DropTable(
                name: "shipping_zone_departments",
                schema: "shipping");

            migrationBuilder.DropTable(
                name: "shipping_zones",
                schema: "shipping");
        }
    }
}
