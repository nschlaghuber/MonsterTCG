using MonsterTCG.Model;
using MonsterTCG.Model.Package;
using Npgsql;
using MonsterTCG.Util;

namespace MonsterTCG.Repository
{
    public class PackageRepository : IPackageRepository
    {
        private readonly NpgsqlDataSource _dataSource;
        private readonly ICardRepository _cardRepository;

        public PackageRepository(NpgsqlDataSource dataSource, ICardRepository cardRepository)
        {
            _dataSource = dataSource;
            _cardRepository = cardRepository;
        }

        public async Task<Package?> FindPackageByIdAsync(string id)
        {
            await using var connection = await _dataSource.OpenConnectionAsync();
            await using var findPackageCommand = new NpgsqlCommand(
                "SELECT * FROM package " +
                "WHERE package_id = @packageId",
                connection);

            findPackageCommand.Parameters.AddWithValue("@packageId", id);

            await findPackageCommand.PrepareAsync();
            await using var findPackageReader = await findPackageCommand.ExecuteReaderAsync();

            if (!await findPackageReader.ReadAsync())
            {
                return null;
            }

            var packageCards = (await _cardRepository.FindCardsByIdsAsync(new[]
            {
                findPackageReader.GetString(1),
                findPackageReader.GetString(2),
                findPackageReader.GetString(3),
                findPackageReader.GetString(4),
                findPackageReader.GetString(5),
            }))?.ToList();

            return packageCards is not null
                ? new Package(findPackageReader.GetString(0), packageCards[0], packageCards[1], packageCards[2],
                    packageCards[3], packageCards[4])
                : null;
        }

        public async Task<Package?> FindPackageByCardIdsAsync((string?, string?, string?, string?, string?) cardIds)
        {
            var cardIdList = TupleUtil.GetListFromTuple<string>(cardIds);

            await using var connection = await _dataSource.OpenConnectionAsync();
            await using var findPackageCommand = new NpgsqlCommand(
                $"SELECT * FROM package " +
                $"WHERE {string.Join(" AND ", Enumerable.Range(0, cardIdList.Count).Select(i => $"card_id_{i + 1} = @cardId{i + 1}"))}",
                connection);

            findPackageCommand.Parameters.AddRange(cardIdList
                .Select((id, i) => new NpgsqlParameter($"@cardId{i + 1}", id)).ToArray());

            await findPackageCommand.PrepareAsync();
            await using var findPackageReader = await findPackageCommand.ExecuteReaderAsync();

            if (!await findPackageReader.ReadAsync())
            {
                return null;
            }

            var packageCards = (await _cardRepository.FindCardsByIdsAsync(new[]
            {
                findPackageReader.GetString(1),
                findPackageReader.GetString(2),
                findPackageReader.GetString(3),
                findPackageReader.GetString(4),
                findPackageReader.GetString(5),
            }))?.ToList();

            return packageCards is null
                ? new Package(findPackageReader.GetString(0), packageCards[0], packageCards[1], packageCards[2],
                    packageCards[3], packageCards[4])
                : null;
        }

        public async Task<Package?> FindFirstPackageAsync()
        {
            await using var connection = await _dataSource.OpenConnectionAsync();
            await using var findFirstPackageCommand = new NpgsqlCommand($"SELECT * FROM package LIMIT 1", connection);

            await using var findFirstPackageReader = await findFirstPackageCommand.ExecuteReaderAsync();

            if (await findFirstPackageReader.ReadAsync())
            {
                var cards = (await _cardRepository.FindCardsByIdsAsync(new List<string>
                {
                    findFirstPackageReader.GetString(1),
                    findFirstPackageReader.GetString(2),
                    findFirstPackageReader.GetString(3),
                    findFirstPackageReader.GetString(4),
                    findFirstPackageReader.GetString(5),
                }))!.ToList();

                return new Package(
                findFirstPackageReader.GetString(0),
                cards[0],
                cards[1],
                cards[2],
                cards[3],
                cards[4]
                );
            }

            return null;
        }

        public async Task<bool> ExistsByCardIdsAsync((string?, string?, string?, string?, string?) cardIds)
        {
            return await FindPackageByCardIdsAsync(cardIds) is not null;
        }

        public async Task CreatePackageAsync(Package package)
        {
            var cardsToAdd = package.CardList;

            var existingCards = (await _cardRepository.FindCardsByIdsAsync(package.CardList.Select(c => c.Id)))?.ToList();

            if (existingCards is null)
            {
                cardsToAdd = package.CardList.Except(existingCards).ToList();
            }

            await _cardRepository.CreateCardsAsync(cardsToAdd);

            await using var connection = await _dataSource.OpenConnectionAsync();
            await using var createPackageCommand = new NpgsqlCommand(
                $"INSERT INTO package (package_id, card_id_1, card_id_2, card_id_3, card_id_4, card_id_5) " +
                $"VALUES (@packageId, @cardId1, @cardId2, @cardId3, @cardId4, @cardId5)",
                connection);

            createPackageCommand.Parameters.AddWithValue("@packageId", package.PackageId);
            createPackageCommand.Parameters.AddRange(package.CardList
                .Select((card, i) => new NpgsqlParameter($"@cardId{i + 1}", card.Id)).ToArray());

            await createPackageCommand.PrepareAsync();
            await createPackageCommand.ExecuteNonQueryAsync();
        }

        public async Task<bool> DeletePackageByIdAsync(string packageId)
        {
            await using var connection = await _dataSource.OpenConnectionAsync();
            await using var deletePackageCommand = new NpgsqlCommand(
                "DELETE FROM package\n" +
                "WHERE package_id = @packageId",
                connection);

            deletePackageCommand.Parameters.AddWithValue("@packageId", packageId);

            await deletePackageCommand.PrepareAsync();
            await deletePackageCommand.ExecuteNonQueryAsync();

            return true;
        }
    }
}