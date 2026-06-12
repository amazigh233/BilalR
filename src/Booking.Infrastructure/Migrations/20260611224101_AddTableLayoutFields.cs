using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Booking.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddTableLayoutFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "PosX",
                table: "Tables",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "PosY",
                table: "Tables",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Rotation",
                table: "Tables",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Shape",
                table: "Tables",
                type: "int",
                nullable: false,
                defaultValue: 0);

            // Spread existing tables across a 5-column grid so none overlap.
            migrationBuilder.Sql(@"
                WITH numbered AS (
                    SELECT Id, (ROW_NUMBER() OVER (PARTITION BY RestaurantId ORDER BY Name) - 1) AS rn
                    FROM Tables
                )
                UPDATE Tables
                SET PosX = (numbered.rn % 5) * 150,
                    PosY = (numbered.rn / 5) * 150
                FROM numbered
                WHERE Tables.Id = numbered.Id;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PosX",
                table: "Tables");

            migrationBuilder.DropColumn(
                name: "PosY",
                table: "Tables");

            migrationBuilder.DropColumn(
                name: "Rotation",
                table: "Tables");

            migrationBuilder.DropColumn(
                name: "Shape",
                table: "Tables");
        }
    }
}
