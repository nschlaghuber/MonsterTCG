using MonsterTCG.Model.Card;

namespace MonsterTCG.Model.Trade;

public class Trade : IEquatable<Trade>
{
    public string Id { get; private init; } = null!;
    public Card.Card CardToTrade { get; private init; } = null!;
    public CardType Type { get; private init; }
    public int MinimumDamage { get; private init; }

    private Trade() { }

    public static Trade Create(string id, Card.Card cardToTrade, CardType type, int minimumDamage)
    {
        return new Trade
        {
            Id = id,
            CardToTrade = cardToTrade,
            Type = type,
            MinimumDamage = minimumDamage,
        };
    }

    public bool Equals(Trade? other)
    {
        if (ReferenceEquals(null, other)) return false;
        if (ReferenceEquals(this, other)) return true;
        return Id == other.Id;
    }

    public override bool Equals(object? obj)
    {
        if (ReferenceEquals(null, obj)) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != this.GetType()) return false;
        return Equals((Trade)obj);
    }

    public override int GetHashCode()
    {
        return Id.GetHashCode();
    }
}