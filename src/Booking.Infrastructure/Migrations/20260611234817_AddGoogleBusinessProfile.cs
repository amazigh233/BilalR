using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Booking.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddGoogleBusinessProfile : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "GoogleBusinessConnections",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RestaurantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    OAuthState = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    GbpAccountName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    GbpLocationName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    EncryptedAccessToken = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    EncryptedRefreshToken = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AccessExpiresAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastReviewSyncAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastHoursSyncAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastSyncError = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GoogleBusinessConnections", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GoogleBusinessConnections_Restaurants_RestaurantId",
                        column: x => x.RestaurantId,
                        principalTable: "Restaurants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "GoogleReviews",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RestaurantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ReviewName = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    ReviewerDisplayName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    StarRating = table.Column<int>(type: "int", nullable: false),
                    Comment = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreateTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdateTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ReplyComment = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ReplyUpdateTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsRead = table.Column<bool>(type: "bit", nullable: false),
                    SyncedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GoogleReviews", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GoogleReviews_Restaurants_RestaurantId",
                        column: x => x.RestaurantId,
                        principalTable: "Restaurants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_GoogleBusinessConnections_RestaurantId",
                table: "GoogleBusinessConnections",
                column: "RestaurantId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_GoogleReviews_RestaurantId_CreateTime",
                table: "GoogleReviews",
                columns: new[] { "RestaurantId", "CreateTime" });

            migrationBuilder.CreateIndex(
                name: "IX_GoogleReviews_RestaurantId_ReviewName",
                table: "GoogleReviews",
                columns: new[] { "RestaurantId", "ReviewName" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "GoogleBusinessConnections");

            migrationBuilder.DropTable(
                name: "GoogleReviews");
        }
    }
}
