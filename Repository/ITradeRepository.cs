using MonsterTCG.Model.Trade;

namespace MonsterTCG.Repository;

public interface ITradeRepository
{
    public Task<IEnumerable<Trade>> AllAsync();
    public Task<Trade?> FindTradeByIdAsync(string id);
    public Task<IEnumerable<Trade>> FindTradesByCreatorUserIdAsync(string creatorUserId);
    public Task<string?> FindCreatorUserIdByTradeId(string tradeId);
    public Task<bool> ExistsByIdAsync(string id);
    public Task CreateTradeAsync(Trade trade, string creatorUserId);
    public Task DeleteTradeByIdAsync(string tradeId);
}