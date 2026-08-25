using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProcureLive.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddAnalyticsViews : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                CREATE VIEW "AuctionAnalyticsView" AS
                WITH bid_stats AS (
                    SELECT
                        ai."AuctionId" AS "AuctionId",
                        b."Id" AS "BidId",
                        b."CompanyId",
                        b."Amount",
                        b."BidTime",
                        ROW_NUMBER() OVER (PARTITION BY ai."AuctionId" ORDER BY b."BidTime" ASC) AS "BidSequenceAsc",
                        ROW_NUMBER() OVER (PARTITION BY ai."AuctionId" ORDER BY b."BidTime" DESC) AS "BidSequenceDesc"
                    FROM "Bids" b
                    INNER JOIN "AuctionItems" ai ON ai."Id" = b."AuctionItemId"
                )
                SELECT
                    a."Id" AS "AuctionId",
                    a."Title",
                    a."AuctionType",
                    a."Status",
                    a."OwnerCompanyId",
                    a."StartAt",
                    a."EndAt",
                    COALESCE(bc."TotalBidCount", 0) AS "TotalBidCount",
                    COALESCE(bc."UniqueBidderCount", 0) AS "UniqueBidderCount",
                    COALESCE(ext."ExtensionCount", 0) AS "ExtensionCount",
                    first_bid."Amount" AS "FirstBidAmount",
                    first_bid."BidTime" AS "FirstBidAt",
                    last_bid."Amount" AS "LastBidAmount",
                    CASE
                        WHEN first_bid."Amount" IS NOT NULL AND first_bid."Amount" <> 0
                            THEN ROUND((((first_bid."Amount" - last_bid."Amount") / first_bid."Amount") * 100)::numeric, 2)
                        ELSE NULL
                    END AS "PriceChangePercentage",
                    CASE
                        WHEN first_bid."BidTime" IS NOT NULL
                            THEN EXTRACT(EPOCH FROM (first_bid."BidTime" - a."StartAt")) / 60.0
                        ELSE NULL
                    END AS "MinutesToFirstBid"
                FROM "Auctions" a
                LEFT JOIN (
                    SELECT "AuctionId", COUNT(*) AS "TotalBidCount", COUNT(DISTINCT "CompanyId") AS "UniqueBidderCount"
                    FROM bid_stats
                    GROUP BY "AuctionId"
                ) bc ON bc."AuctionId" = a."Id"
                LEFT JOIN bid_stats first_bid ON first_bid."AuctionId" = a."Id" AND first_bid."BidSequenceAsc" = 1
                LEFT JOIN bid_stats last_bid ON last_bid."AuctionId" = a."Id" AND last_bid."BidSequenceDesc" = 1
                LEFT JOIN (
                    SELECT "AuctionId", COUNT(*) AS "ExtensionCount"
                    FROM "AuctionExtensionLogs"
                    GROUP BY "AuctionId"
                ) ext ON ext."AuctionId" = a."Id";
                """);

            migrationBuilder.Sql(
                """
                CREATE VIEW "SupplierPerformanceView" AS
                SELECT
                    c."Id" AS "CompanyId",
                    c."Name" AS "CompanyName",
                    rfi_stats."AverageRfiScore",
                    COALESCE(rfi_stats."RfiEvaluationCount", 0) AS "RfiEvaluationCount",
                    COALESCE(rfq_stats."SubmittedRfqQuoteCount", 0) AS "SubmittedRfqQuoteCount",
                    COALESCE(auction_stats."TotalAuctionsParticipated", 0) AS "TotalAuctionsParticipated",
                    COALESCE(award_stats."WonAuctionCount", 0) AS "WonAuctionCount",
                    CASE
                        WHEN COALESCE(auction_stats."TotalAuctionsParticipated", 0) > 0
                            THEN ROUND((COALESCE(award_stats."WonAuctionCount", 0)::numeric / auction_stats."TotalAuctionsParticipated") * 100, 2)
                        ELSE NULL
                    END AS "AuctionWinRatePercentage",
                    invitation_response."AverageResponseHours"
                FROM "Companies" c
                LEFT JOIN (
                    SELECT "CompanyId", AVG("TotalScore") AS "AverageRfiScore", COUNT(*) AS "RfiEvaluationCount"
                    FROM "SupplierEvaluations"
                    GROUP BY "CompanyId"
                ) rfi_stats ON rfi_stats."CompanyId" = c."Id"
                LEFT JOIN (
                    SELECT "CompanyId", COUNT(*) AS "SubmittedRfqQuoteCount"
                    FROM "RfqQuotes"
                    WHERE "Status" = 1
                    GROUP BY "CompanyId"
                ) rfq_stats ON rfq_stats."CompanyId" = c."Id"
                LEFT JOIN (
                    SELECT "CompanyId", COUNT(DISTINCT "AuctionId") AS "TotalAuctionsParticipated"
                    FROM "AuctionInvitations"
                    WHERE "Status" IN (3, 4, 5)
                    GROUP BY "CompanyId"
                ) auction_stats ON auction_stats."CompanyId" = c."Id"
                LEFT JOIN (
                    SELECT "AwardedCompanyId" AS "CompanyId", COUNT(*) AS "WonAuctionCount"
                    FROM "AuctionAwards"
                    GROUP BY "AwardedCompanyId"
                ) award_stats ON award_stats."CompanyId" = c."Id"
                LEFT JOIN (
                    SELECT "CompanyId", AVG(EXTRACT(EPOCH FROM ("RespondedAt" - "InvitedAt")) / 3600.0) AS "AverageResponseHours"
                    FROM "AuctionInvitations"
                    WHERE "RespondedAt" IS NOT NULL
                    GROUP BY "CompanyId"
                ) invitation_response ON invitation_response."CompanyId" = c."Id";
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP VIEW IF EXISTS \"SupplierPerformanceView\";");
            migrationBuilder.Sql("DROP VIEW IF EXISTS \"AuctionAnalyticsView\";");
        }
    }
}
