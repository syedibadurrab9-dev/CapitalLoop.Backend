using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class initialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AdvisorConsultations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProspectName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ProspectEmail = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    TargetedAssetClass = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    RequestedCallSlotUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsAssignedToAgent = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    AssignedAgentEmail = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedByEmail = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    LastModifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastModifiedByEmail = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AdvisorConsultations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ChatMessages",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SenderEmail = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    ReceiverEmail = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    MessageText = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    ChartTokenPayload = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsAcknowledgedByOperator = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    IsRead = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedByEmail = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    LastModifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastModifiedByEmail = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChatMessages", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ExchangeRates",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FromCurrency = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    ToCurrency = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    Rate = table.Column<decimal>(type: "decimal(18,6)", nullable: false),
                    LastUpdatedUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedByEmail = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    LastModifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastModifiedByEmail = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExchangeRates", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MarketAssetPrices",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AssetTicker = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    AssetName = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    AssetClass = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    UnitPricePkr = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    LastSyncedUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedByEmail = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    LastModifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastModifiedByEmail = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MarketAssetPrices", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PortfolioShareLinks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OwnerEmail = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    RecipientEmail = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    AccessToken = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: false),
                    ExpiresAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsRevoked = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedByEmail = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    LastModifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastModifiedByEmail = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PortfolioShareLinks", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TaxAuditJobs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserEmail = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    InitiatedBySystem = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    ExecutionStatus = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false, defaultValue: "Queued"),
                    ProcessedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedByEmail = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    LastModifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastModifiedByEmail = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TaxAuditJobs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IdentityProviderId = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    DisplayName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    SubscriptionTier = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    SubscriptionUpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UserProfileId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedByEmail = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    LastModifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastModifiedByEmail = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "UserProfiles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Cnic = table.Column<string>(type: "nchar(13)", fixedLength: true, maxLength: 13, nullable: true),
                    IsFiler = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    Age = table.Column<int>(type: "int", nullable: false),
                    SubscriptionTier = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    KycStatus = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    RiskMatrix = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    SuitabilityAssessmentScore = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    SuitabilityAssessmentCompletedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ApplicationUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedByEmail = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    LastModifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastModifiedByEmail = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserProfiles", x => x.Id);
                    table.UniqueConstraint("AK_UserProfiles_Email", x => x.Email);
                    table.ForeignKey(
                        name: "FK_UserProfiles_Users_ApplicationUserId",
                        column: x => x.ApplicationUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AssetNodes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserEmail = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Category = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ValuationPkr = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    IsAutoSynced = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    Symbol = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    QuantityHoldings = table.Column<decimal>(type: "decimal(18,6)", nullable: false),
                    InstallmentsPaid = table.Column<int>(type: "int", nullable: true),
                    TotalInstallments = table.Column<int>(type: "int", nullable: true),
                    CostPerTola = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    EncryptedApiKey = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedByEmail = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    LastModifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastModifiedByEmail = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AssetNodes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AssetNodes_UserProfiles_UserEmail",
                        column: x => x.UserEmail,
                        principalTable: "UserProfiles",
                        principalColumn: "Email",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "KycDocuments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserProfileId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserEmail = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    FrontImagePath = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    BackImagePath = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    SanitizedCnic = table.Column<string>(type: "nchar(13)", fixedLength: true, maxLength: 13, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    RejectionReason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    AuditedByAgentEmail = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    AuditedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedByEmail = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    LastModifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastModifiedByEmail = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KycDocuments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_KycDocuments_UserProfiles_UserProfileId",
                        column: x => x.UserProfileId,
                        principalTable: "UserProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AdvisorConsultations_AssignedAgentEmail",
                table: "AdvisorConsultations",
                column: "AssignedAgentEmail",
                filter: "[AssignedAgentEmail] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_AdvisorConsultations_ProspectEmail",
                table: "AdvisorConsultations",
                column: "ProspectEmail");

            migrationBuilder.CreateIndex(
                name: "IX_AdvisorConsultations_Schedule_Clash_Composite",
                table: "AdvisorConsultations",
                columns: new[] { "AssignedAgentEmail", "RequestedCallSlotUtc" },
                filter: "[AssignedAgentEmail] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_AssetNodes_MarketSync_Covering_Composite",
                table: "AssetNodes",
                columns: new[] { "UserEmail", "Symbol", "IsAutoSynced" });

            migrationBuilder.CreateIndex(
                name: "IX_AssetNodes_UserEmail",
                table: "AssetNodes",
                column: "UserEmail");

            migrationBuilder.CreateIndex(
                name: "IX_ChatMessages_ReceiverEmail",
                table: "ChatMessages",
                column: "ReceiverEmail");

            migrationBuilder.CreateIndex(
                name: "IX_ChatMessages_SenderEmail",
                table: "ChatMessages",
                column: "SenderEmail");

            migrationBuilder.CreateIndex(
                name: "IX_ChatMessages_Thread_Chronological_Composite",
                table: "ChatMessages",
                columns: new[] { "SenderEmail", "ReceiverEmail", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_ChatMessages_Unacknowledged_Queue_Sparse_Composite",
                table: "ChatMessages",
                column: "IsAcknowledgedByOperator",
                filter: "[IsAcknowledgedByOperator] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_ChatMessages_Unread_Badge_Sparse_Composite",
                table: "ChatMessages",
                columns: new[] { "ReceiverEmail", "IsRead" },
                filter: "[IsRead] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_ExchangeRates_CurrencyPair_Unique_Composite",
                table: "ExchangeRates",
                columns: new[] { "FromCurrency", "ToCurrency" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_KycDocuments_AuditedByAgentEmail",
                table: "KycDocuments",
                column: "AuditedByAgentEmail",
                filter: "[AuditedByAgentEmail] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_KycDocuments_SanitizedCnic",
                table: "KycDocuments",
                column: "SanitizedCnic");

            migrationBuilder.CreateIndex(
                name: "IX_KycDocuments_Status_Billing_Telemetry",
                table: "KycDocuments",
                columns: new[] { "Status", "UserEmail" });

            migrationBuilder.CreateIndex(
                name: "IX_KycDocuments_UserEmail",
                table: "KycDocuments",
                column: "UserEmail");

            migrationBuilder.CreateIndex(
                name: "IX_KycDocuments_UserProfileId",
                table: "KycDocuments",
                column: "UserProfileId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MarketAssetPrices_AssetTicker_Unique",
                table: "MarketAssetPrices",
                column: "AssetTicker",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MarketAssetPrices_Ticker_SyncHistory_Composite",
                table: "MarketAssetPrices",
                columns: new[] { "AssetTicker", "LastSyncedUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_PortfolioShareLinks_OwnerEmail",
                table: "PortfolioShareLinks",
                column: "OwnerEmail");

            migrationBuilder.CreateIndex(
                name: "IX_PortfolioShareLinks_RecipientEmail",
                table: "PortfolioShareLinks",
                column: "RecipientEmail");

            migrationBuilder.CreateIndex(
                name: "UX_PortfolioShareLinks_AccessToken",
                table: "PortfolioShareLinks",
                column: "AccessToken",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TaxAuditJobs_QueueProcessor_Composite",
                table: "TaxAuditJobs",
                columns: new[] { "ExecutionStatus", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_TaxAuditJobs_UserEmail",
                table: "TaxAuditJobs",
                column: "UserEmail");

            migrationBuilder.CreateIndex(
                name: "IX_UserProfiles_ApplicationUserId",
                table: "UserProfiles",
                column: "ApplicationUserId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserProfiles_Compliance_Telemetry",
                table: "UserProfiles",
                columns: new[] { "KycStatus", "IsFiler" });

            migrationBuilder.CreateIndex(
                name: "IX_UserProfiles_Email_Unique",
                table: "UserProfiles",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_Email_Unique",
                table: "Users",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_IdentityProviderId_Unique",
                table: "Users",
                column: "IdentityProviderId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AdvisorConsultations");

            migrationBuilder.DropTable(
                name: "AssetNodes");

            migrationBuilder.DropTable(
                name: "ChatMessages");

            migrationBuilder.DropTable(
                name: "ExchangeRates");

            migrationBuilder.DropTable(
                name: "KycDocuments");

            migrationBuilder.DropTable(
                name: "MarketAssetPrices");

            migrationBuilder.DropTable(
                name: "PortfolioShareLinks");

            migrationBuilder.DropTable(
                name: "TaxAuditJobs");

            migrationBuilder.DropTable(
                name: "UserProfiles");

            migrationBuilder.DropTable(
                name: "Users");
        }
    }
}
