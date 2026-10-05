using MediatR;
using Microsoft.EntityFrameworkCore;
using ProcureLive.Application.Common.Currencies;
using ProcureLive.Application.Common.Exceptions;
using ProcureLive.Application.Common.Interfaces;
using ProcureLive.Contracts.Rfq;
using ProcureLive.Domain.Entities.Rfq;
using ProcureLive.Domain.Enums;

namespace ProcureLive.Application.Rfq.Queries.GetRfqAiRecommendation;

public sealed class GetRfqAiRecommendationQueryHandler
    : IRequestHandler<GetRfqAiRecommendationQuery, RfqAiRecommendationDto>
{
    private readonly IApplicationDbContext _context;

    public GetRfqAiRecommendationQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<RfqAiRecommendationDto> Handle(
        GetRfqAiRecommendationQuery request,
        CancellationToken cancellationToken)
    {
        RfqProcess? process = await _context.RfqProcesses
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == request.RfqProcessId, cancellationToken);

        if (process is null)
            throw new NotFoundException(nameof(RfqProcess), request.RfqProcessId);

        // Load submitted quotes with their line items and quantities
        List<RfqQuote> quotes = await _context.RfqQuotes
            .AsNoTracking()
            .Include(q => q.Company)
            .Include(q => q.LineItems).ThenInclude(li => li.RfqLineItem)
            .Where(q => q.RfqProcessId == request.RfqProcessId && q.Status == RfqQuoteStatus.Submitted)
            .ToListAsync(cancellationToken);

        if (quotes.Count == 0)
            throw new ConflictException("Öneri oluşturmak için en az bir gönderilmiş teklif gereklidir.");

        // Compute per-quote aggregates
        var quoteMetrics = new List<(RfqQuote Quote, decimal TotalCost, double? AvgLeadDays)>();

        foreach (RfqQuote quote in quotes)
        {
            decimal totalCost = 0;
            var leadDays = new List<int>();

            foreach (RfqQuoteLineItem li in quote.LineItems)
            {
                decimal normalized = await CurrencyNormalizer.NormalizeToBaseCurrencyAsync(
                    _context, li.UnitPrice.Amount, li.UnitPrice.CurrencyId, cancellationToken);
                totalCost += normalized * li.RfqLineItem.Quantity;

                if (li.LeadTimeDays.HasValue)
                    leadDays.Add(li.LeadTimeDays.Value);
            }

            double? avgLead = leadDays.Count > 0 ? leadDays.Average() : (double?)null;
            quoteMetrics.Add((quote, totalCost, avgLead));
        }

        // Price scoring (70%)
        decimal minCost = quoteMetrics.Min(m => m.TotalCost);
        decimal maxCost = quoteMetrics.Max(m => m.TotalCost);
        decimal costRange = maxCost - minCost;

        // Lead time scoring (30%)
        var leadValues = quoteMetrics.Where(m => m.AvgLeadDays.HasValue).Select(m => m.AvgLeadDays!.Value).ToList();
        double? minLead = leadValues.Count > 0 ? leadValues.Min() : (double?)null;
        double? maxLead = leadValues.Count > 0 ? leadValues.Max() : (double?)null;
        bool hasLeadData = minLead.HasValue && maxLead.HasValue && maxLead.Value > minLead.Value;

        var scores = new List<RfqQuoteScoreDto>();
        foreach ((RfqQuote quote, decimal totalCost, double? avgLead) in quoteMetrics)
        {
            decimal priceScore = costRange == 0
                ? 100m
                : Math.Round((maxCost - totalCost) / costRange * 100m, 1);

            decimal leadScore;
            if (!hasLeadData)
                leadScore = 50m;
            else if (!avgLead.HasValue)
                leadScore = 50m;
            else
                leadScore = Math.Round((decimal)((maxLead!.Value - avgLead.Value) / (maxLead.Value - minLead!.Value) * 100), 1);

            decimal totalScore = Math.Round(priceScore * 0.7m + leadScore * 0.3m, 1);

            scores.Add(new RfqQuoteScoreDto(
                quote.Id, quote.Company.Name, totalScore, priceScore, leadScore, totalCost, avgLead));
        }

        RfqQuoteScoreDto best = scores.OrderByDescending(s => s.TotalScore).First();
        RfqQuoteScoreDto? secondBest = scores.OrderByDescending(s => s.TotalScore).Skip(1).FirstOrDefault();

        decimal confidenceScore = secondBest is null
            ? 100m
            : Math.Min(100m, Math.Round((best.TotalScore - secondBest.TotalScore) * 2.5m + 60m, 1));

        string rationale = BuildRationale(best, secondBest, hasLeadData, scores.Count);

        return new RfqAiRecommendationDto(
            RecommendedQuoteId: quotes.First(q => q.Company.Name == best.CompanyName).Id,
            RecommendedCompanyName: best.CompanyName,
            ConfidenceScore: confidenceScore,
            Rationale: rationale,
            Scores: scores.OrderByDescending(s => s.TotalScore).ToList());
    }

    private static string BuildRationale(
        RfqQuoteScoreDto best,
        RfqQuoteScoreDto? secondBest,
        bool hasLeadData,
        int totalQuotes)
    {
        var parts = new List<string>();

        parts.Add($"{best.CompanyName} şirketi, {totalQuotes} teklif arasında en yüksek puanı ({best.TotalScore:F1}/100) aldı.");

        if (best.PriceScore >= 80)
            parts.Add("Fiyat açısından diğer tekliflere kıyasla belirgin biçimde avantajlı.");
        else if (best.PriceScore >= 50)
            parts.Add("Fiyat açısından rekabetçi bir konumda.");
        else
            parts.Add("Fiyat avantajı sınırlı olmakla birlikte diğer kriterler bu boşluğu telafi ediyor.");

        if (hasLeadData)
        {
            if (best.LeadTimeScore >= 70)
                parts.Add("Teslim süresi diğer tedarikçilere göre kısa.");
            else if (best.LeadTimeScore >= 50)
                parts.Add("Teslim süresi sektör ortalamasına yakın.");
        }
        else
        {
            parts.Add("Teslim süresi verisi mevcut olmadığından fiyat ağırlıklı değerlendirme yapıldı.");
        }

        if (secondBest is not null)
        {
            decimal gap = best.TotalScore - secondBest.TotalScore;
            if (gap < 5)
                parts.Add($"Ancak {secondBest.CompanyName} ({secondBest.TotalScore:F1}/100) yakın bir alternatif; ek değerlendirme önerilebilir.");
            else
                parts.Add($"İkinci sıradaki {secondBest.CompanyName} ({secondBest.TotalScore:F1}/100) ile aradaki puan farkı belirgin.");
        }

        return string.Join(" ", parts);
    }
}
