using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Booking.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddWidgetBranding : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "WidgetAccentColor",
                table: "Restaurants",
                type: "nvarchar(7)",
                maxLength: 7,
                nullable: false,
                defaultValue: "#b97742");

            migrationBuilder.AddColumn<string>(
                name: "WidgetLogoUrl",
                table: "Restaurants",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "WidgetPrimaryColor",
                table: "Restaurants",
                type: "nvarchar(7)",
                maxLength: 7,
                nullable: false,
                defaultValue: "#1f6655");

            migrationBuilder.AddColumn<string>(
                name: "WidgetWelcomeText",
                table: "Restaurants",
                type: "nvarchar(240)",
                maxLength: 240,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "WidgetAccentColor",
                table: "Restaurants");

            migrationBuilder.DropColumn(
                name: "WidgetLogoUrl",
                table: "Restaurants");

            migrationBuilder.DropColumn(
                name: "WidgetPrimaryColor",
                table: "Restaurants");

            migrationBuilder.DropColumn(
                name: "WidgetWelcomeText",
                table: "Restaurants");
        }
    }
}
