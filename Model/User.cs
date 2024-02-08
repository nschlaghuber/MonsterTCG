using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MonsterTCG.Util;

namespace MonsterTCG.Model
{
    public record UserData(string Name, string Bio, string Image);

    public record UserStats(int EloScore, int Wins, int Losses);
    public class User
    {
        public string Id { get; }
        public string Username { get; }
        public string Password { get; }
        public UserData UserData { get; }
        public List<Card> Collection { get; }
        public Deck Deck { get; set; }
        public int Coins { get; set; }
        public UserStats UserStats { get; }
        
        public List<Card?> DeckCardsAsList => TupleUtil.GetListFromTuple<Card>(Deck.Cards);

        public User(string username, string password)
        {
            Id = Guid.NewGuid().ToString();
            Username = username;
            Password = password;
            UserData = new UserData("", "", "");
            Collection = new List<Card>();
            Deck = new Deck(Guid.NewGuid().ToString(), (null, null, null, null));
            Coins = 20;
            UserStats = new UserStats(500, 0, 0);
        }

        public User(string id, string username, string password, UserData userData, List<Card> collection, Deck deck, int coins, UserStats userStats)
        {
            Id = id;
            Username = username;
            Password = password;
            UserData = userData;
            Collection = collection;
            Deck = deck;
            Coins = coins;
            UserStats = userStats;
        }
    }
}
