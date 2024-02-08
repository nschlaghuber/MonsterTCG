using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace MonsterTCG.Model
{
    public enum ElementType
    {
        Fire,
        Water,
        Normal,
    }

    public enum CardType
    {
        Spell,
        Monster,
    }

    public class Card : IEquatable<Card>
    {
        public string Id { get; }
        
        public string Name { get; }
        
        public int Damage { get; }
        
        [JsonConverter(typeof(StringEnumConverter))]
        public ElementType ElementType { get; }
        
        [JsonConverter(typeof(StringEnumConverter))]
        public CardType CardType { get; }

        public Card(string id, string name, int damage, ElementType elementType, CardType cardType)
        {
            Id = id;
            Name = name;
            Damage = damage;
            ElementType = elementType;
            CardType = cardType;
        }

        public bool Equals(Card? other)
        {
            return Id.Equals(other?.Id) &&
                   Name.Equals(other?.Name) &&
                   Damage.Equals(other.Damage) &&
                   ElementType.Equals(other.ElementType) &&
                   CardType.Equals(other.CardType);
        }

        public override bool Equals(object? obj)
        {
            return Equals(obj as Card);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Id, Name, Damage, (int)ElementType, (int)CardType);
        }
    }
}