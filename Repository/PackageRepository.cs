using MonsterTCG.Model;
using Npgsql;
using MonsterTCG.Util;

namespace MonsterTCG.Repository
{
    public class PackageRepository : Repository
    {
        private readonly CardRepository _cardRepository;

        public PackageRepository(NpgsqlDataSource dataSource, CardRepository cardRepository) : base(dataSource)
        {
            this._cardRepository = cardRepository;
        }

        public async Task<Package?> FindPackageByCardIds((string?, string?, string?, string?, string?) cardIds)
        {
            var cardIdList = TupleUtil.GetListFromTuple<string>(cardIds);

            await using var connection = await DataSource.OpenConnectionAsync();
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

            var packageCards = (await _cardRepository.FindCardsByIds(new[]
            {
                findPackageReader.GetString(1),
                findPackageReader.GetString(2),
                findPackageReader.GetString(3),
                findPackageReader.GetString(4),
                findPackageReader.GetString(5),
            }))?.ToList();

            return packageCards != null
                ? new Package(findPackageReader.GetString(0), packageCards[0], packageCards[1], packageCards[2],
                    packageCards[3], packageCards[4])
                : null;
        }

        public async Task<Package?> FindPackageById(string id)
        {
            await using var connection = await DataSource.OpenConnectionAsync();
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

            var packageCards = (await _cardRepository.FindCardsByIds(new[]
            {
                findPackageReader.GetString(1),
                findPackageReader.GetString(2),
                findPackageReader.GetString(3),
                findPackageReader.GetString(4),
                findPackageReader.GetString(5),
            }))?.ToList();

            return packageCards != null
                ? new Package(findPackageReader.GetString(0), packageCards[0], packageCards[1], packageCards[2],
                    packageCards[3], packageCards[4])
                : null;
        }

        public async Task<Package?> FindRandomPackage()
        {
            await using var connection = await DataSource.OpenConnectionAsync();
            await using var findAllPackageIdsCommand = new NpgsqlCommand($"SELECT package_id FROM package", connection);

            await using var findAllPackageIdsReader = await findAllPackageIdsCommand.ExecuteReaderAsync();

            if (!findAllPackageIdsReader.HasRows)
            {
                return null;
            }

            var allPackageIds = new List<string>();

            while (await findAllPackageIdsReader.ReadAsync())
            {
                allPackageIds.Add(findAllPackageIdsReader.GetString(0));
            }

            var rnd = new Random();

            var randomPackageId = allPackageIds[rnd.Next(0, allPackageIds.Count)];

            return await FindPackageById(randomPackageId);
        }

        public async Task<bool> ExistsByCardIds((string?, string?, string?, string?, string?) cardIds)
        {
            return (await FindPackageByCardIds(cardIds)) != null;
        }

        public async Task<Package?> CreatePackage(Package package)
        {
            var cardsToAdd = package.CardList;

            var existingCards = (await _cardRepository.FindCardsByIds(package.CardList.Select(c => c.Id)))?.ToList();

            if (existingCards != null)
            {
                cardsToAdd = package.CardList.Except(existingCards).ToList();
            }

            foreach (var card in cardsToAdd)
            {
                if (await _cardRepository.CreateCard(card!) == null)
                {
                    return null;
                }
            }

            await using var connection = await DataSource.OpenConnectionAsync();
            await using var createPackageCommand = new NpgsqlCommand(
                $"INSERT INTO package (package_id, card_id_1, card_id_2, card_id_3, card_id_4, card_id_5) " +
                $"VALUES (@packageId, @cardId1, @cardId2, @cardId3, @cardId4, @cardId5)",
                connection);

            createPackageCommand.Parameters.AddWithValue("@packageId", package.PackageId);
            createPackageCommand.Parameters.AddRange(package.CardList
                .Select((card, i) => new NpgsqlParameter($"@cardId{i + 1}", card.Id)).ToArray());

            await createPackageCommand.PrepareAsync();
            await createPackageCommand.ExecuteNonQueryAsync();

            return package;
        }

        public async Task<bool> DeletePackageById(string packageId)
        {
            await using var connection = await DataSource.OpenConnectionAsync();
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