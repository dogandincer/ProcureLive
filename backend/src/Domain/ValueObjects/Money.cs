namespace ProcureLive.Domain.ValueObjects;

/// <summary>
/// Multi-currency monetary amount. Persisted as an EF Core owned type (OwnsOne)
/// on the entities that need it (Bid.Amount, Auction.ReservePrice, RfqQuoteLineItem.UnitPrice, ...).
/// </summary>
public sealed class Money : IEquatable<Money>
{
    public decimal Amount { get; private set; }

    public Guid CurrencyId { get; private set; }

    private Money()
    {
    }

    public Money(decimal amount, Guid currencyId)
    {
        if (amount < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(amount), "Money amount cannot be negative.");
        }

        if (currencyId == Guid.Empty)
        {
            throw new ArgumentException("Money must have a currency.", nameof(currencyId));
        }

        Amount = amount;
        CurrencyId = currencyId;
    }

    public static Money Zero(Guid currencyId) => new(0, currencyId);

    public bool Equals(Money? other)
    {
        if (other is null)
        {
            return false;
        }

        return Amount == other.Amount && CurrencyId == other.CurrencyId;
    }

    public override bool Equals(object? obj) => Equals(obj as Money);

    public override int GetHashCode() => HashCode.Combine(Amount, CurrencyId);

    public override string ToString() => $"{Amount} ({CurrencyId})";
}
