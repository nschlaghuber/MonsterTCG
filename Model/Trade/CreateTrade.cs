using MonsterTCG.Model.Card;

namespace MonsterTCG.Model.Trade;

public record CreateTrade(string Id, string CardToTrade, CardType Type, int MinimumDamage);