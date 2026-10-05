using Microsoft.EntityFrameworkCore;
using ProcureLive.Domain.Entities.Analytics;
using ProcureLive.Domain.Entities.Audit;
using ProcureLive.Domain.Entities.Auctions;
using ProcureLive.Domain.Entities.Catalog;
using ProcureLive.Domain.Entities.Companies;
using ProcureLive.Domain.Entities.Finance;
using ProcureLive.Domain.Entities.Identity;
using ProcureLive.Domain.Entities.Notifications;
using ProcureLive.Domain.Entities.Rfi;
using ProcureLive.Domain.Entities.Rfq;

namespace ProcureLive.Application.Common.Interfaces;

/// <summary>
/// Read-side abstraction over the EF Core context. Query handlers project directly
/// off these IQueryable sets (ProjectTo&lt;TDto&gt;) instead of going through repositories.
/// </summary>
public interface IApplicationDbContext
{
    DbSet<Company> Companies { get; }

    DbSet<ContactPerson> ContactPersons { get; }

    DbSet<Address> Addresses { get; }

    DbSet<CompanySupplierCategory> CompanySupplierCategories { get; }

    DbSet<Sector> Sectors { get; }

    DbSet<CompanySector> CompanySectors { get; }

    DbSet<Category> Categories { get; }

    DbSet<Product> Products { get; }

    DbSet<ProductImage> ProductImages { get; }

    DbSet<ProductSpecification> ProductSpecifications { get; }

    DbSet<Currency> Currencies { get; }

    DbSet<ExchangeRate> ExchangeRates { get; }

    DbSet<Auction> Auctions { get; }

    DbSet<AuctionItem> AuctionItems { get; }

    DbSet<AuctionInvitation> AuctionInvitations { get; }

    DbSet<Bid> Bids { get; }

    DbSet<AuctionExtensionLog> AuctionExtensionLogs { get; }

    DbSet<AuctionAward> AuctionAwards { get; }

    DbSet<RfiTemplate> RfiTemplates { get; }

    DbSet<RfiQuestion> RfiQuestions { get; }

    DbSet<RfiQuestionOption> RfiQuestionOptions { get; }

    DbSet<RfiProcess> RfiProcesses { get; }

    DbSet<RfiInvitation> RfiInvitations { get; }

    DbSet<RfiResponse> RfiResponses { get; }

    DbSet<SupplierEvaluation> SupplierEvaluations { get; }

    DbSet<RfqProcess> RfqProcesses { get; }

    DbSet<RfqLineItem> RfqLineItems { get; }

    DbSet<RfqInvitation> RfqInvitations { get; }

    DbSet<RfqQuote> RfqQuotes { get; }

    DbSet<RfqQuoteLineItem> RfqQuoteLineItems { get; }

    DbSet<Notification> Notifications { get; }

    DbSet<NotificationChannelLog> NotificationChannelLogs { get; }

    DbSet<InvitationResponseLog> InvitationResponseLogs { get; }

    DbSet<AuditLog> AuditLogs { get; }

    DbSet<MlFeatureSnapshot> MlFeatureSnapshots { get; }

    DbSet<RefreshToken> RefreshTokens { get; }

    DbSet<Permission> Permissions { get; }

    DbSet<RolePermission> RolePermissions { get; }

    /// <summary>Read-only, keyless projection of the "AuctionAnalyticsView" database view.</summary>
    DbSet<AuctionAnalyticsViewRow> AuctionAnalytics { get; }

    /// <summary>Read-only, keyless projection of the "SupplierPerformanceView" database view.</summary>
    DbSet<SupplierPerformanceViewRow> SupplierPerformance { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Executes a raw SQL statement (e.g. "SELECT 1 FROM ... FOR UPDATE" row locks) against the
    /// current ambient transaction. Parameters are positional ({0}, {1}, ...).
    /// </summary>
    Task ExecuteRawSqlAsync(string sql, object[] parameters, CancellationToken cancellationToken);
}
