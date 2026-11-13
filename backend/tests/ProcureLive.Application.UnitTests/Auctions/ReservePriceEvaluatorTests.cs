using FluentAssertions;
using ProcureLive.Application.Auctions.Reserve;
using ProcureLive.Domain.Entities.Auctions;
using ProcureLive.Domain.Enums;
using ProcureLive.Domain.ValueObjects;

namespace ProcureLive.Application.UnitTests.Auctions;

public class ReservePriceEvaluatorTests
{
    private static ReserveLine Line(decimal quantity, decimal? unitPrice) => new(quantity, unitPrice);

    [Theory]
    [InlineData(null)]
    [InlineData(0)]
    public void Rezerv_tanimli_degilse_NotSet(int? reserve)
    {
        ReserveEvaluation result = ReservePriceEvaluator.Evaluate(reserve, higherIsBetter: false, new[] { Line(10, 5) });

        result.Status.Should().Be(ReserveStatus.NotSet);
        result.Total.Should().BeNull();
    }

    [Fact]
    public void Hic_teklif_yoksa_NoBids()
    {
        ReserveEvaluation result = ReservePriceEvaluator.Evaluate(1000, higherIsBetter: false, new[] { Line(10, null), Line(2, null) });

        result.Status.Should().Be(ReserveStatus.NoBids);
        result.CoveredItems.Should().Be(0);
        result.ItemCount.Should().Be(2);
    }

    [Fact]
    public void Dusuk_teklif_kazanir_toplam_rezervin_altinda_ve_tum_kalemler_teklifli_ise_Met()
    {
        // 10 × 40 + 5 × 100 = 900 ≤ 1000
        ReserveEvaluation result = ReservePriceEvaluator.Evaluate(1000, higherIsBetter: false, new[] { Line(10, 40), Line(5, 100) });

        result.Status.Should().Be(ReserveStatus.Met);
        result.Total.Should().Be(900);
    }

    [Fact]
    public void Dusuk_teklif_kazanir_toplam_rezerve_esitse_Met()
    {
        ReserveEvaluation result = ReservePriceEvaluator.Evaluate(900, higherIsBetter: false, new[] { Line(10, 40), Line(5, 100) });

        result.Status.Should().Be(ReserveStatus.Met);
    }

    [Fact]
    public void Dusuk_teklif_kazanir_toplam_rezervi_asarsa_NotMet()
    {
        ReserveEvaluation result = ReservePriceEvaluator.Evaluate(899.99m, higherIsBetter: false, new[] { Line(10, 40), Line(5, 100) });

        result.Status.Should().Be(ReserveStatus.NotMet);
    }

    [Fact]
    public void Dusuk_teklif_kazanir_teklifsiz_kalem_varken_toplam_rezervin_altindaysa_Incomplete()
    {
        ReserveEvaluation result = ReservePriceEvaluator.Evaluate(1000, higherIsBetter: false, new[] { Line(10, 40), Line(5, null) });

        result.Status.Should().Be(ReserveStatus.Incomplete);
        result.Total.Should().Be(400);
        result.CoveredItems.Should().Be(1);
    }

    [Fact]
    public void Dusuk_teklif_kazanir_eksik_kalemle_bile_rezerv_asildiysa_NotMet()
    {
        ReserveEvaluation result = ReservePriceEvaluator.Evaluate(300, higherIsBetter: false, new[] { Line(10, 40), Line(5, null) });

        result.Status.Should().Be(ReserveStatus.NotMet);
    }

    [Fact]
    public void Yuksek_teklif_kazanir_toplam_rezerve_ulasirsa_Met_ulasmazsa_NotMet()
    {
        ReserveLine[] lines = { Line(10, 40), Line(5, null) };

        ReservePriceEvaluator.Evaluate(400, higherIsBetter: true, lines).Status.Should().Be(ReserveStatus.Met);
        ReservePriceEvaluator.Evaluate(401, higherIsBetter: true, lines).Status.Should().Be(ReserveStatus.NotMet);
    }

    [Fact]
    public void Kapali_zarfta_en_iyi_teklif_tutara_gore_secilir_esitlikte_ilk_verilen_ondedir()
    {
        Guid currency = Guid.NewGuid();
        DateTime t0 = new(2026, 1, 1, 10, 0, 0, DateTimeKind.Utc);
        var late = new Bid { Amount = new Money(90, currency), BidTime = t0.AddMinutes(5), Status = BidStatus.Winning };
        var early = new Bid { Amount = new Money(90, currency), BidTime = t0, Status = BidStatus.Winning };
        var high = new Bid { Amount = new Money(120, currency), BidTime = t0.AddMinutes(1), Status = BidStatus.Winning };

        BidRanking.Best(new[] { high, late, early }, AuctionType.SealedBid).Should().BeSameAs(early);
    }

    [Fact]
    public void Kapali_zarfta_geri_cekilmis_ve_reddedilmis_teklif_en_iyi_sayilmaz()
    {
        Guid currency = Guid.NewGuid();
        var withdrawn = new Bid { Amount = new Money(50, currency), Status = BidStatus.Withdrawn };
        var rejected = new Bid { Amount = new Money(60, currency), Status = BidStatus.Rejected };
        var valid = new Bid { Amount = new Money(90, currency), Status = BidStatus.Winning };

        BidRanking.Best(new[] { withdrawn, rejected, valid }, AuctionType.SealedBid).Should().BeSameAs(valid);
        BidRanking.Best(new[] { withdrawn, rejected }, AuctionType.SealedBid).Should().BeNull();
    }

    [Fact]
    public void Acik_ihalede_en_iyi_teklif_Winning_durumundaki_tekliftir()
    {
        Guid currency = Guid.NewGuid();
        var outbid = new Bid { Amount = new Money(80, currency), Status = BidStatus.Outbid };
        var winning = new Bid { Amount = new Money(100, currency), Status = BidStatus.Winning };

        BidRanking.Best(new[] { outbid, winning }, AuctionType.English).Should().BeSameAs(winning);
        BidRanking.Best(Array.Empty<Bid>(), AuctionType.English).Should().BeNull();
    }
}
