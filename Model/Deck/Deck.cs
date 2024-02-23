namespace MonsterTCG.Model.Deck;

public class Deck
{
    public string Id { get; }
    public (Card.Card? card1, Card.Card? card2, Card.Card? card3, Card.Card? card4) Cards { get; set; }

    public Deck(string id)
    {
        Id = id;
        Cards = (null, null, null, null);
    }
    
    public Deck(string id, (Card.Card card1, Card.Card card2, Card.Card card3, Card.Card card4) cards)
    {
        Id = id;
        Cards = cards;
    }
}