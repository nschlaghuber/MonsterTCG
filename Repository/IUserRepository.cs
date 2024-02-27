using MonsterTCG.Model.Card;
using MonsterTCG.Model.Deck;
using MonsterTCG.Model.User;

namespace MonsterTCG.Repository;

public interface IUserRepository
{
    public Task<IEnumerable<User>> AllAsync();
    public Task<User?> FindByIdAsync(string id);
    public Task<User?> FindByUsernameAsync(string username);
    public Task<Deck?> FindDeckFromIdAsync(string deckId);
    public Task<bool> ExistsByUsernameAsync(string username);
    public Task<bool> HasCardFromIdAsync(User user, string cardId);
    public Task<bool> HasCardsFromIdsAsync(User user, List<string> cardIds);
    public Task<bool> AuthorizeUserAsync(UserCredentials providedCredentials);
    public Task CreateUserAsync(User user);
    public Task UpdateUserAsync(User user);
}