using MonsterTCG.Model.Card;

namespace MonsterTCG.Repository;

public interface ICardRepository
{
    public Task<Card?> FindCardByIdAsync(string id);
    public Task<IEnumerable<Card>> FindCardsByIdsAsync(IEnumerable<string> ids);
    public Task<bool> ExistsByIdAsync(string id);
    public Task<bool> ExistAnyByIdsAsync(IEnumerable<string> ids);
    public Task<Card?> CreateCardAsync(Card card);
    public Task CreateCardsAsync(IEnumerable<Card> cards);
}