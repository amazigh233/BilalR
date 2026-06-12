using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Booking.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAccountingLightModule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AccountingCategories",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RestaurantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    EntryType = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    IsDefault = table.Column<bool>(type: "bit", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AccountingCategories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AccountingCategories_Restaurants_RestaurantId",
                        column: x => x.RestaurantId,
                        principalTable: "Restaurants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AccountingConnections",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RestaurantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Provider = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    ExternalId = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    EncryptedAccessToken = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    EncryptedRefreshToken = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AccessExpiresAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastSyncedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastError = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AccountingConnections", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AccountingConnections_Restaurants_RestaurantId",
                        column: x => x.RestaurantId,
                        principalTable: "Restaurants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AccountingImportBatches",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RestaurantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ImportKind = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    FileName = table.Column<string>(type: "nvarchar(260)", maxLength: 260, nullable: false),
                    FileChecksum = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    MappingJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    StorageKey = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    RowCount = table.Column<int>(type: "int", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AccountingImportBatches", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AccountingImportBatches_Restaurants_RestaurantId",
                        column: x => x.RestaurantId,
                        principalTable: "Restaurants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AccountingSourceTransactions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RestaurantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SourceKind = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    ExternalId = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Fingerprint = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    TransactionDate = table.Column<DateOnly>(type: "date", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Currency = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: false),
                    RawPayload = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ImportBatchId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    AccountingEntryId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ReviewedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AccountingSourceTransactions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AccountingSourceTransactions_AccountingImportBatches_ImportBatchId",
                        column: x => x.ImportBatchId,
                        principalTable: "AccountingImportBatches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AccountingSourceTransactions_Restaurants_RestaurantId",
                        column: x => x.RestaurantId,
                        principalTable: "Restaurants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AccountingEntries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RestaurantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EntryType = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    EntryDate = table.Column<DateOnly>(type: "date", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    Currency = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: false),
                    GrossAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    SourceTransactionId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CorrectionOfEntryId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ConfirmedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CorrectedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Splits = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AccountingEntries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AccountingEntries_AccountingEntries_CorrectionOfEntryId",
                        column: x => x.CorrectionOfEntryId,
                        principalTable: "AccountingEntries",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AccountingEntries_AccountingSourceTransactions_SourceTransactionId",
                        column: x => x.SourceTransactionId,
                        principalTable: "AccountingSourceTransactions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AccountingEntries_Restaurants_RestaurantId",
                        column: x => x.RestaurantId,
                        principalTable: "Restaurants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AccountingMatches",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RestaurantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BankSourceTransactionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MatchedSourceTransactionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AccountingMatches", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AccountingMatches_AccountingSourceTransactions_BankSourceTransactionId",
                        column: x => x.BankSourceTransactionId,
                        principalTable: "AccountingSourceTransactions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AccountingMatches_AccountingSourceTransactions_MatchedSourceTransactionId",
                        column: x => x.MatchedSourceTransactionId,
                        principalTable: "AccountingSourceTransactions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AccountingMatches_Restaurants_RestaurantId",
                        column: x => x.RestaurantId,
                        principalTable: "Restaurants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AccountingAttachments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RestaurantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AccountingEntryId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FileName = table.Column<string>(type: "nvarchar(260)", maxLength: 260, nullable: false),
                    ContentType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    StorageKey = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Checksum = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    Length = table.Column<long>(type: "bigint", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AccountingAttachments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AccountingAttachments_AccountingEntries_AccountingEntryId",
                        column: x => x.AccountingEntryId,
                        principalTable: "AccountingEntries",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AccountingAttachments_Restaurants_RestaurantId",
                        column: x => x.RestaurantId,
                        principalTable: "Restaurants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AccountingAttachments_AccountingEntryId",
                table: "AccountingAttachments",
                column: "AccountingEntryId");

            migrationBuilder.CreateIndex(
                name: "IX_AccountingAttachments_RestaurantId_AccountingEntryId",
                table: "AccountingAttachments",
                columns: new[] { "RestaurantId", "AccountingEntryId" });

            migrationBuilder.CreateIndex(
                name: "IX_AccountingCategories_RestaurantId_EntryType_Name",
                table: "AccountingCategories",
                columns: new[] { "RestaurantId", "EntryType", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AccountingConnections_RestaurantId_Provider_ExternalId",
                table: "AccountingConnections",
                columns: new[] { "RestaurantId", "Provider", "ExternalId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AccountingEntries_CorrectionOfEntryId",
                table: "AccountingEntries",
                column: "CorrectionOfEntryId");

            migrationBuilder.CreateIndex(
                name: "IX_AccountingEntries_RestaurantId_EntryDate",
                table: "AccountingEntries",
                columns: new[] { "RestaurantId", "EntryDate" });

            migrationBuilder.CreateIndex(
                name: "IX_AccountingEntries_SourceTransactionId",
                table: "AccountingEntries",
                column: "SourceTransactionId");

            migrationBuilder.CreateIndex(
                name: "IX_AccountingImportBatches_RestaurantId_FileChecksum",
                table: "AccountingImportBatches",
                columns: new[] { "RestaurantId", "FileChecksum" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AccountingMatches_BankSourceTransactionId",
                table: "AccountingMatches",
                column: "BankSourceTransactionId");

            migrationBuilder.CreateIndex(
                name: "IX_AccountingMatches_MatchedSourceTransactionId",
                table: "AccountingMatches",
                column: "MatchedSourceTransactionId");

            migrationBuilder.CreateIndex(
                name: "IX_AccountingMatches_RestaurantId_BankSourceTransactionId",
                table: "AccountingMatches",
                columns: new[] { "RestaurantId", "BankSourceTransactionId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AccountingSourceTransactions_ImportBatchId",
                table: "AccountingSourceTransactions",
                column: "ImportBatchId");

            migrationBuilder.CreateIndex(
                name: "IX_AccountingSourceTransactions_RestaurantId_Fingerprint",
                table: "AccountingSourceTransactions",
                columns: new[] { "RestaurantId", "Fingerprint" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AccountingSourceTransactions_RestaurantId_Status_TransactionDate",
                table: "AccountingSourceTransactions",
                columns: new[] { "RestaurantId", "Status", "TransactionDate" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AccountingAttachments");

            migrationBuilder.DropTable(
                name: "AccountingCategories");

            migrationBuilder.DropTable(
                name: "AccountingConnections");

            migrationBuilder.DropTable(
                name: "AccountingMatches");

            migrationBuilder.DropTable(
                name: "AccountingEntries");

            migrationBuilder.DropTable(
                name: "AccountingSourceTransactions");

            migrationBuilder.DropTable(
                name: "AccountingImportBatches");
        }
    }
}
