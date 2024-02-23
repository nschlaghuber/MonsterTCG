using System.Data;
using MonsterTCG.Model;
using MonsterTCG.Model.Card;
using MonsterTCG.Model.SQL;
using Npgsql;
using NpgsqlTypes;

namespace MonsterTCG.Repository
{
    public class CardRepository : ICardRepository
    {
        private readonly NpgsqlDataSource _dataSource;

        public CardRepository(NpgsqlDataSource dataSource)
        {
            _dataSource = dataSource;
        }

        public async Task<Card?> FindCardByIdAsync(string id)
        {
            await using var connection = await _dataSource.OpenConnectionAsync();
            await using var findCardCommand = new NpgsqlCommand(
                "SELECT card_id, name, damage, element_type, card_type FROM card WHERE card_id = @cardId",
                connection);

            findCardCommand.Parameters.AddWithValue("@cardId", id);

            await findCardCommand.PrepareAsync();
            await using var findCardReader = await findCardCommand.ExecuteReaderAsync();

            if (!await findCardReader.ReadAsync())
            {
                return null;
            }
            
            return Card.Create(
                findCardReader.GetString("card_id"),
                findCardReader.GetString("name"),
                findCardReader.GetInt32("damage"),
                Enum.Parse<ElementType>(findCardReader.GetString("element_type")),
                Enum.Parse<CardType>(findCardReader.GetString("card_type"))
            );
        }

        public async Task<IEnumerable<Card>> FindCardsByIdsAsync(IEnumerable<string> ids)
        {
            await using var connection = await _dataSource.OpenConnectionAsync();
            await using var findCardCommand = new NpgsqlCommand(
                $"SELECT * FROM card WHERE card_id = ANY(@ids)",
                connection);

            findCardCommand.Parameters.AddWithValue("@ids", ids.ToArray());

            await findCardCommand.PrepareAsync();
            await using var findCardReader = await findCardCommand.ExecuteReaderAsync();

            var cards = new List<Card>();

            while (await findCardReader.ReadAsync())
            {
                cards.Add(Card.Create(
                    findCardReader.GetString(0),
                    findCardReader.GetString(1),
                    findCardReader.GetInt32(2),
                    Enum.Parse<ElementType>(findCardReader.GetString(3)),
                    Enum.Parse<CardType>(findCardReader.GetString(4)))
                );
            }

            return cards;
        }

        public async Task<bool> ExistsByIdAsync(string id)
        {
            return (await FindCardByIdAsync(id)) != null;
        }

        public async Task<bool> ExistAnyByIdsAsync(IEnumerable<string> ids)
        {
            var foundCards = await FindCardsByIdsAsync(ids);
            return foundCards.Any();
        }

        public async Task<Card?> CreateCardAsync(Card card)
        {
            try
            {
                await using var connection = await _dataSource.OpenConnectionAsync();
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

        public async Task CreateCardsAsync(IEnumerable<Card> cards)
        {
            var cardsList = cards.ToList();

            if (!cardsList.Any())
            {
                return;
            }

            await using var connection = await _dataSource.OpenConnectionAsync();
            await using var createCardCommand = new NpgsqlCommand(
                $"INSERT INTO card (card_id, name, damage, element_type, card_type)\n" +
                $"VALUES\n" + string.Join(",\n",
                    cardsList.Select((_, i) =>
                        $"(@cardId{i + 1}, @name{i + 1}, @damage{i + 1}, @elementType{i + 1}, @cardType{i + 1})")),
                connection);

            createCardCommand.Parameters.AddRange(cardsList.SelectMany((card, i) =>
                new List<NpgsqlParameter>
                {
                    new($"@cardId{i + 1}", card.Id),
                    new($"@name{i + 1}", card.Name),
                    new($"@damage{i + 1}", card.Damage),
                    new($"@elementType{i + 1}", card.ElementType.ToString()),
                    new($"@cardType{i + 1}", card.CardType.ToString()),
                }).ToArray());

            await createCardCommand.PrepareAsync();
            await createCardCommand.ExecuteNonQueryAsync();
        }
    }
}