using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CommerceHub.Modules.Orders.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddNationalShipping : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "delivery_mode",
                schema: "orders",
                table: "orders",
                type: "character varying(10)",
                maxLength: 10,
                nullable: false,
                defaultValue: "Delivery");

            migrationBuilder.AddColumn<string>(
                name: "department_code",
                schema: "orders",
                table: "orders",
                type: "character varying(10)",
                maxLength: 10,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "pickup_store_id",
                schema: "orders",
                table: "orders",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "pickup_store_name",
                schema: "orders",
                table: "orders",
                type: "character varying(150)",
                maxLength: 150,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "shipping_total",
                schema: "orders",
                table: "orders",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "delivery_mode",
                schema: "orders",
                table: "orders");

            migrationBuilder.DropColumn(
                name: "department_code",
                schema: "orders",
                table: "orders");

            migrationBuilder.DropColumn(
                name: "pickup_store_id",
                schema: "orders",
                table: "orders");

            migrationBuilder.DropColumn(
                name: "pickup_store_name",
                schema: "orders",
                table: "orders");

            migrationBuilder.DropColumn(
                name: "shipping_total",
                schema: "orders",
                table: "orders");
        }
    }
}
