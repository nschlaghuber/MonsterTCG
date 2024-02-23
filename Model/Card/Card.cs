using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace MonsterTCG.Model.Card
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
        public string Id { get; private init; } = null!;

        public string Name { get; private init; } = null!;

        public int Damage { get; private init; }
        
        [JsonConverter(typeof(StringEnumConverter))]
        public ElementType ElementType { get; private init; }
        
        [JsonConverter(typeof(StringEnumConverter))]
        public CardType CardType { get; private init; }
        
        private Card() { }

        public static Card Create(string id, string name, int damage)
        {
            return new Card
            {
                Id = id,
                Name = name,
                Damage = damage,
                ElementType =
                    name.Contains("Fire") ? ElementType.Fire :
                    name.Contains("Water") ? ElementType.Water :
                    ElementType.Normal,
                CardType =
                    name.Contains("Spell") ? CardType.Spell : CardType.Monster,
            };
        }

        public static Card Create(string id, string name, int damage, ElementType elementType, CardType cardType)
        {
            return new Card
            {
                Id = id,
                Name = name,
                Damage = damage,
                ElementType = elementType,
                CardType = cardType,
            };
        }

        public override string ToString()
        {
            return JsonConvert.SerializeObject(this, Formatting.Indented);
        }

        public bool Equals(Card? other)
        {
            return Id.Equals(other?.Id);
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