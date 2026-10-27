using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using ProcureLive.Application.Common.Interfaces;
using ProcureLive.Domain.Entities.Analytics;
using ProcureLive.Domain.Entities.Audit;
using ProcureLive.Domain.Entities.Auctions;
using ProcureLive.Domain.Entities.Catalog;
using ProcureLive.Domain.Entities.Companies;
using ProcureLive.Domain.Entities.Finance;
using ProcureLive.Domain.Entities.Workspaces;
using ProcureLive.Domain.Entities.Identity;
using ProcureLive.Domain.Entities.Notifications;
using ProcureLive.Domain.Entities.Rfi;
using ProcureLive.Domain.Entities.Rfq;
using ProcureLive.Domain.Entities.Subscriptions;
using ProcureLive.Infrastructure.Identity;
using ProcureLive.Infrastructure.Persistence.Interceptors;

namespace ProcureLive.Infrastructure.Persistence;

public class ApplicationDbContext
    : IdentityDbContext<ApplicationUser, ApplicationRole, Guid>, IApplicationDbContext
{
    private readonly AuditableEntitySaveChangesInterceptor _auditInterceptor;

    public ApplicationDbContext(
        DbContextOptions<ApplicationDbContext> options,
        AuditableEntitySaveChangesInterceptor auditInterceptor)
        : base(options)
    {
        _auditInterceptor = auditInterceptor;
    }

    public DbSet<Company> Companies => Set<Company>();

    public DbSet<CompanyEvaluation> CompanyEvaluations => Set<CompanyEvaluation>();

    public DbSet<ContactPerson> ContactPersons => Set<ContactPerson>();

    public DbSet<Address> Addresses => Set<Address>();

    public DbSet<CompanySupplierCategory> CompanySupplierCategories => Set<CompanySupplierCategory>();

    public DbSet<Sector> Sectors => Set<Sector>();

    public DbSet<CompanySector> CompanySectors => Set<CompanySector>();

    public DbSet<Workspace> Workspaces => Set<Workspace>();

    public DbSet<UserWorkspaceAccess> UserWorkspaceAccesses => Set<UserWorkspaceAccess>();

    public DbSet<CompanyWorkspace> CompanyWorkspaces => Set<CompanyWorkspace>();

    public DbSet<ParticipationRequest> ParticipationRequests => Set<ParticipationRequest>();

    public DbSet<ParticipationRequestLine> ParticipationRequestLines => Set<ParticipationRequestLine>();

    public DbSet<Category> Categories => Set<Category>();

    public DbSet<Product> Products => Set<Product>();

    public DbSet<ProductImage> ProductImages => Set<ProductImage>();

    public DbSet<ProductSpecification> ProductSpecifications => Set<ProductSpecification>();

    public DbSet<Currency> Currencies => Set<Currency>();

    public DbSet<ExchangeRate> ExchangeRates => Set<ExchangeRate>();

    public DbSet<Auction> Auctions => Set<Auction>();

    public DbSet<AuctionItem> AuctionItems => Set<AuctionItem>();

    public DbSet<AuctionInvitation> AuctionInvitations => Set<AuctionInvitation>();

    public DbSet<Bid> Bids => Set<Bid>();

    public DbSet<AuctionExtensionLog> AuctionExtensionLogs => Set<AuctionExtensionLog>();

    public DbSet<AuctionStatusChange> AuctionStatusChanges => Set<AuctionStatusChange>();

    public DbSet<AuctionAward> AuctionAwards => Set<AuctionAward>();

    public DbSet<AuctionAttachment> AuctionAttachments => Set<AuctionAttachment>();

    public DbSet<InvitationToken> InvitationTokens => Set<InvitationToken>();

    public DbSet<CompanySubscription> CompanySubscriptions => Set<CompanySubscription>();

    public DbSet<RfiTemplate> RfiTemplates => Set<RfiTemplate>();

    public DbSet<RfiQuestion> RfiQuestions => Set<RfiQuestion>();

    public DbSet<RfiQuestionOption> RfiQuestionOptions => Set<RfiQuestionOption>();

    public DbSet<RfiProcess> RfiProcesses => Set<RfiProcess>();

    public DbSet<RfiInvitation> RfiInvitations => Set<RfiInvitation>();

    public DbSet<RfiResponse> RfiResponses => Set<RfiResponse>();

    public DbSet<SupplierEvaluation> SupplierEvaluations => Set<SupplierEvaluation>();

    public DbSet<RfqProcess> RfqProcesses => Set<RfqProcess>();

    public DbSet<RfqLineItem> RfqLineItems => Set<RfqLineItem>();

    public DbSet<RfqInvitation> RfqInvitations => Set<RfqInvitation>();

    public DbSet<RfqQuote> RfqQuotes => Set<RfqQuote>();

    public DbSet<RfqQuoteLineItem> RfqQuoteLineItems => Set<RfqQuoteLineItem>();

    public DbSet<Notification> Notifications => Set<Notification>();

    public DbSet<NotificationChannelLog> NotificationChannelLogs => Set<NotificationChannelLog>();

    public DbSet<InvitationResponseLog> InvitationResponseLogs => Set<InvitationResponseLog>();

    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    public DbSet<MlFeatureSnapshot> MlFeatureSnapshots => Set<MlFeatureSnapshot>();

    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    public DbSet<Permission> Permissions => Set<Permission>();

    public DbSet<RolePermission> RolePermissions => Set<RolePermission>();

    public DbSet<AuctionAnalyticsViewRow> AuctionAnalytics => Set<AuctionAnalyticsViewRow>();

    public DbSet<SupplierPerformanceViewRow> SupplierPerformance => Set<SupplierPerformanceViewRow>();

    public Task ExecuteRawSqlAsync(string sql, object[] parameters, CancellationToken cancellationToken)
    {
        return Database.ExecuteSqlRawAsync(sql, parameters, cancellationToken);
    }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);

        builder.Entity<Company>().HasQueryFilter(c => !c.IsDeleted);
        builder.Entity<Product>().HasQueryFilter(p => !p.IsDeleted);

        builder.Entity<AuctionAnalyticsViewRow>(v =>
        {
            v.HasNoKey();
            v.ToView("AuctionAnalyticsView");
        });

        builder.Entity<SupplierPerformanceViewRow>(v =>
        {
            v.HasNoKey();
            v.ToView("SupplierPerformanceView");
        });
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.AddInterceptors(_auditInterceptor);
        base.OnConfiguring(optionsBuilder);
    }
}
