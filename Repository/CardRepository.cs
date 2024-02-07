using MonsterTCG.Model;
using MonsterTCG.Model.SQL;
using Npgsql;
using NpgsqlTypes;

namespace MonsterTCG.Repository
{
    public class CardRepository : Repository
    {
        public CardRepository(NpgsqlDataSource dataSource) : base(dataSource) { }

        public async Task<Card?> FindCardById(string id)
        {
            try
            {
                await using var connection = await DataSource.OpenConnectionAsync();
                await using var findCardCommand = new NpgsqlCommand(
                    $"SELECT * FROM card WHERE \"id\" = @id",
                    connection);

                findCardCommand.Parameters.AddWithValue("@id", id);

                await findCardCommand.PrepareAsync();
                await using var findCardReader = await findCardCommand.ExecuteReaderAsync();

                if (await findCardReader.ReadAsync())
                {
                    return new Card(
                        findCardReader.GetString(1),
                        findCardReader.GetString(2),
                        findCardReader.GetInt32(3),
                        Enum.Parse<ElementType>(findCardReader.GetString(4)),
                        Enum.Parse<CardType>(findCardReader.GetString(5)));
                }

                return null;
            }
            catch (Exception)
            {
                return null;
            }
        }
        
        public async Task<IEnumerable<Card>?> FindCardsByIds(IEnumerable<string> ids)
        {
            try
            {
                await using var connection = await DataSource.OpenConnectionAsync();
                await using var findCardCommand = new NpgsqlCommand(
                    $"SELECT * FROM card WHERE card_id = ANY(@ids)",
                    connection);

                findCardCommand.Parameters.AddWithValue("@ids", ids.ToArray());

                await findCardCommand.PrepareAsync();
                await using var findCardReader = await findCardCommand.ExecuteReaderAsync();

                var cards = new List<Card>();

                while (await findCardReader.ReadAsync())
                {
                    var card = new Card(
                        findCardReader.GetString(0),
                        findCardReader.GetString(1),
                        findCardReader.GetInt32(2),
                        Enum.Parse<ElementType>(findCardReader.GetString(3)),
                        Enum.Parse<CardType>(findCardReader.GetString(4)));

                    cards.Add(card);
                }

                return cards;
            }
            catch (Exception)
            {
                return null;
            }
        }

        public async Task<bool> ExistsById(string id)
        {
            return (await FindCardById(id)) != null;
        }

        public async Task<bool> ExistAnyByIds(IEnumerable<string> ids)
        {
            var foundCards = await FindCardsByIds(ids);
            return foundCards != null && foundCards.Any();
        }

        public async Task<Card?> CreateCard(Card card)
        {
            try
            {
                await using var connection = await DataSource.OpenConnectionAsync();
                await using var createCardCommand = new NpgsqlCommand(
                    $"INSERT INTO card (card_id, name, damage, element_type, card_type) " +
                    $"VALUES (@cardId, @name, @damage, @elementType, @cardType)",
                    connection);

                createCardCommand.Parameters.AddWithValue("@cardId", card.Id);
                createCardCommand.Parameters.AddWithValue("@name", card.Name);
                createCardCommand.Parameters.AddWithValue("@damage", card.Damage);
                createCardCommand.Parameters.AddWithValue("@elementType", card.ElementType.ToString());
                createCardCommand.Parameters.AddWithValue("@cardType", card.CardType.ToString());

                await createCardCommand.PrepareAsync();
                await createCardCommand.ExecuteNonQueryAsync();

                return card;
            }
            catch (Exception)
            {
                return null;
            }
        }
    }
}