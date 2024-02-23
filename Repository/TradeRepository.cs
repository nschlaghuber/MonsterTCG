using System.Data;
using MonsterTCG.Model.Card;
using MonsterTCG.Model.Trade;
using Npgsql;

namespace MonsterTCG.Repository;

public class TradeRepository : ITradeRepository
{
    private readonly NpgsqlDataSource _dataSource;
    private readonly ICardRepository _cardRepository;

    public TradeRepository(NpgsqlDataSource dataSource, ICardRepository cardRepository)
    {
        _dataSource = dataSource;
        _cardRepository = cardRepository;
    }


    public async Task<IEnumerable<Trade>> AllAsync()
    {
        await using var connection = await _dataSource.OpenConnectionAsync();
        await using var allTradesCommand = new NpgsqlCommand(
            "SELECT trade_id, card_to_trade_id, type, minimum_damage FROM trade",
            connection);

        await using var allUsersReader = await allTradesCommand.ExecuteReaderAsync();

        var allTrades = new List<Trade>();

        while (await allUsersReader.ReadAsync())
        {
            allTrades.Add(Trade.Create(
                allUsersReader.GetString("trade_id"),
                (await _cardRepository.FindCardByIdAsync(allUsersReader.GetString("card_to_trade_id")))!,
                Enum.Parse<CardType>(allUsersReader.GetString("type")),
                allUsersReader.GetInt32("minimum_damage")
            ));
        }

        return allTrades;
    }

    public async Task<Trade?> FindTradeByIdAsync(string id)
    {
        await using var connection = await _dataSource.OpenConnectionAsync();
        await using var findTradeCommand = new NpgsqlCommand(
            "SELECT trade_id, card_to_trade_id, type, minimum_damage FROM trade\n" +
            "WHERE trade_id = @tradeId",
            connection);

        findTradeCommand.Parameters.AddWithValue("@tradeId", id);

        await findTradeCommand.PrepareAsync();
        await using var allUsersReader = await findTradeCommand.ExecuteReaderAsync();

        if (!await allUsersReader.ReadAsync())
        {
            return null;
        }

        return Trade.Create(
            allUsersReader.GetString("trade_id"),
            (await _cardRepository.FindCardByIdAsync(allUsersReader.GetString("card_to_trade_id")))!,
            Enum.Parse<CardType>(allUsersReader.GetString("type")),
            allUsersReader.GetInt32("minimum_damage")
        );
    }

    public async Task<IEnumerable<Trade>> FindTradesByCreatorUserIdAsync(string creatorUserId)
    {
        await using var connection = await _dataSource.OpenConnectionAsync();
        await using var findTradesByCreatorUserIdCommand = new NpgsqlCommand(
            "SELECT trade_id, card_to_trade_id, type, minimum_damage FROM trade\n" +
            "WHERE creator_user_id = @creatorUserId",
            connection);

        findTradesByCreatorUserIdCommand.Parameters.AddWithValue("@creatorUserId", creatorUserId);

        await findTradesByCreatorUserIdCommand.PrepareAsync();
        await using var findTradesByCreatorUserIdReader = await findTradesByCreatorUserIdCommand.ExecuteReaderAsync();

        var trades = new List<Trade>();

        while (await findTradesByCreatorUserIdReader.ReadAsync())
        {
            trades.Add(Trade.Create(
                findTradesByCreatorUserIdReader.GetString("trade_id"),
                (await _cardRepository.FindCardByIdAsync(findTradesByCreatorUserIdReader.GetString("card_to_trade_id")))!,
                Enum.Parse<CardType>(findTradesByCreatorUserIdReader.GetString("type")),
                findTradesByCreatorUserIdReader.GetInt32("minimum_damage")
            ));
        }

        return trades;
    }

    public async Task<string?> FindCreatorUserIdByTradeId(string tradeId)
    {
        await using var connection = await _dataSource.OpenConnectionAsync();
        await using var findCreatorUserId = new NpgsqlCommand(
            "SELECT creator_user_id FROM trade\n" +
            "WHERE trade_id = @tradeId",
            connection);

        findCreatorUserId.Parameters.AddWithValue("@tradeId", tradeId);

        await findCreatorUserId.PrepareAsync();
        await using var findCreatorUserIdReader = await findCreatorUserId.ExecuteReaderAsync();

        if (!await findCreatorUserIdReader.ReadAsync())
        {
            return null;
        }

        return findCreatorUserIdReader.GetString("creator_user_id");
    }

    public async Task<bool> ExistsByIdAsync(string id)
    {
        await using var connection = await _dataSource.OpenConnectionAsync();
        await using var findTradeExistsCommand = new NpgsqlCommand(
            "SELECT EXISTS(SELECT 1 FROM trade WHERE trade_id = @tradeId)",
            connection);

        findTradeExistsCommand.Parameters.AddWithValue("@tradeId", id);

        await findTradeExistsCommand.PrepareAsync();
        await using var findTradeExistsReader = await findTradeExistsCommand.ExecuteReaderAsync();

        await findTradeExistsReader.ReadAsync();

        return findTradeExistsReader.GetBoolean(0);
    }

    public async Task CreateTradeAsync(Trade trade, string creatorUserId)
    {
        await using var connection = await _dataSource.OpenConnectionAsync();
        await using var createTradeCommand = new NpgsqlCommand(
            "INSERT INTO trade (trade_id, creator_user_id, card_to_trade_id, type, minimum_damage)\n" +
            "VALUES (@tradeId, @creatorUserId, @cardToTradeId, @type, @minimumDamage)",
            connection);

        createTradeCommand.Parameters.AddWithValue("@tradeId", trade.Id);
        createTradeCommand.Parameters.AddWithValue("@creatorUserId", creatorUserId);
        createTradeCommand.Parameters.AddWithValue("@cardToTradeId", trade.CardToTrade.Id);
        createTradeCommand.Parameters.AddWithValue("@type", trade.Type.ToString());
        createTradeCommand.Parameters.AddWithValue("@minimumDamage", trade.MinimumDamage);

        await createTradeCommand.PrepareAsync();
        await createTradeCommand.ExecuteNonQueryAsync();
    }

    public async Task DeleteTradeByIdAsync(string tradeId)
    {
        await using var connection = await _dataSource.OpenConnectionAsync();
        await using var deleteTradeCommand = new NpgsqlCommand(
            "DELETE FROM trade WHERE trade_id = @tradeId",
            connection);

        deleteTradeCommand.Parameters.AddWithValue("@tradeId", tradeId);

        await deleteTradeCommand.PrepareAsync();
        await deleteTradeCommand.ExecuteNonQueryAsync();
    }
}