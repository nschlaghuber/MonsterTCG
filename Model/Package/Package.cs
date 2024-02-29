using MonsterTCG.Util;

namespace MonsterTCG.Model.Package
{
    public class Package : IEquatable<Package>
    {
        public string PackageId { get; private init; } = null!;

        public (Card.Card Card1, Card.Card Card2, Card.Card Card3, Card.Card Card4, Card.Card Card5) Cards { get; private init; }

        public List<Card.Card> CardList => TupleUtil.GetListFromTuple<Card.Card>(Cards)!;

        private Package()
        {
        }

        public static Package Create(string packageId, Card.Card firstCard, Card.Card secondCard, Card.Card thirdCard, Card.Card fourthCard, Card.Card fifthCard)
        {
            return new Package
            {
                PackageId = packageId,
                Cards = (firstCard, secondCard, thirdCard, fourthCard, fifthCard),
            };
        }

        public bool Equals(Package? other)
        {
            return PackageId.Equals(other?.PackageId);
        }

        public override bool Equals(object? obj)
        {
            return Equals(obj as Package);
        }

        public override int GetHashCode()
        {
            return Cards.GetHashCode();
        }
    }
}