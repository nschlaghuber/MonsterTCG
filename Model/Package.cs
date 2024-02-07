using MonsterTCG.Util;

namespace MonsterTCG.Model
{
    public class Package : IEquatable<Package>
    {
        public string PackageId { get; }
        public (Card Card1, Card Card2, Card Card3, Card Card4, Card Card5) Cards { get; }
        public List<Card> CardList => TupleUtil.GetListFromTuple<Card>(Cards);

        public Package(string packageId, Card firstCard, Card secondCard, Card thirdCard, Card fourthCard, Card fifthCard)
        {
            PackageId = packageId;
            Cards = (firstCard, secondCard, thirdCard, fourthCard, fifthCard);
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
