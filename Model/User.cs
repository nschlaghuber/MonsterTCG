using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MonsterTCG.Model
{
    public record UserData(string Name, string Bio, string Image);

    public record UserStats(int EloScore, int Wins, int Losses);
    public class User
    {
        public string Username { get; }
        public string Password { get; }
        public UserData UserData { get; }
        public List<Card> Collection { get; }
        public (Card? card1, Card? card2, Card? card3, Card? card4) Deck { get; }
        public int Coins { get; set; }
        public UserStats UserStats { get; }

        public User(string username, string password)
        {
            Username = username;
            Password = password;
            UserData = new UserData("", "", "");
            Collection = new List<Card>();
            Deck = (null, null, null, null);
            Coins = 20;
            UserStats = new UserStats(500, 0, 0);
        }

        public User(string username, string password, UserData userData, List<Card> collection, int coins, UserStats userStats)
        {
            Username = username;
            Password = password;
            UserData = userData;
            Collection = collection;
            Coins = coins;
            UserStats = userStats;
        }
    }
}
