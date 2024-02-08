namespace MonsterTCG.Model;

public class Deck
{
    public string Id { get; }
    public (Card? card1, Card? card2, Card? card3, Card? card4) Cards { get; }
    
    public Deck(string id, (Card? card1, Card? card2, Card? card3, Card? card4) cards)
    {
        Id = id;
        Cards = cards;
    }
    
}