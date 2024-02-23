using MonsterTCG.Util;

namespace MonsterTCG.Model.User
{
    public class User
    {
        public string Id { get; private set; } = null!;
        public string Username { get; private set; } = null!;
        public string Password { get; private set; } = null!;
        public UserData UserData { get; private set; } = null!;
        public List<Card.Card> Collection { get; private set; } = null!;
        public Deck.Deck Deck { get; private init; } = null!;
        public int Coins { get;  private set; }
        public UserStats UserStats { get; private set; } = null!;
        public List<Trade.Trade> ActiveTrades { get; private set; } = null!;

        public List<Card.Card?> DeckCardsAsList => TupleUtil.GetListFromTuple<Card.Card>(Deck.Cards);
        
        private User() { }

        public static User Create(string username, string password)
        {
            return new User
            {
                Id = Guid.NewGuid().ToString(),
                Username = username,
                Password = password,
                UserData = new UserData("", "", ""),
                Collection = new List<Card.Card>(),
                Deck = new Deck.Deck(Guid.NewGuid().ToString()),
                Coins = 20,
                UserStats = new UserStats(500, 0, 0),
            };
        }

        public static User Create(string id, string username, string password, UserData userData, List<Card.Card> collection, Deck.Deck deck, int coins, UserStats userStats, List<Trade.Trade> activeTrades)
        {
            return new User
            {
                Id = id,
                Username = username,
                Password = password,
                UserData = userData,
                Collection = collection,
                Deck = deck,
                Coins = coins,
                UserStats = userStats,
                ActiveTrades = activeTrades,
            };
        }

        public void SetCoins(int coins)
        {
            Coins = coins;
        }

        public void SetUserData(UserData userData)
        {
            UserData = userData;
        }

        public void AddEloScore(int eloScore)
        {
            UserStats = new UserStats(UserStats.EloScore + eloScore, UserStats.Wins, UserStats.Losses);
        }

        public void AddWin()
        {
            UserStats = new UserStats(UserStats.EloScore, UserStats.Wins + 1, UserStats.Losses);
        }

        public void AddLoss()
        {
            UserStats = new UserStats(UserStats.EloScore, UserStats.Wins, UserStats.Losses + 1);
        }
    }
}
