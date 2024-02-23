using MonsterTCG.Util;

namespace MonsterTCG.Model.Battle;

public class Player
{
    public string Name { get; }
    public List<Card.Card> Deck { get; }
    public int EloScore { get; }
    public Bet Bet { get; }

    public Player(User.User user, Bet bet)
    {
        Name = user.UserData.Name;
        Deck = new List<Card.Card>(TupleUtil.GetListFromTuple<Card.Card>(user.Deck.Cards)!);
        EloScore = user.UserStats.EloScore;
        Bet = bet;
    }
}