using System.Data;
using Npgsql;
using MonsterTCG.Model;
using MonsterTCG.Model.SQL;
using MonsterTCG.Util;

namespace MonsterTCG.Repository
{
    public class UserRepository : Repository
    {
        public UserRepository(NpgsqlDataSource dataSource) : base(dataSource)
        {
        }

        /// <summary>
        /// Finds a user by their username
        /// </summary>
        /// <param name="username">The username of the user</param>
        /// <returns>The user with the given username. If no such user has been found, returns null</returns>
        public async Task<User?> FindUserByUsername(string username)
        {
            try
            {
                await using var connection = await DataSource.OpenConnectionAsync();
                await using var findUserCommand = new NpgsqlCommand(
                    "SELECT user_id, username, password, name, bio, image, coins, elo_score, wins, losses\n" +
                    "FROM \"user\"\n" +
                    "INNER JOIN user_data ON \"user\".user_data_id = user_data.user_data_id\n" +
                    "INNER JOIN user_stats ON \"user\".user_stats_id = user_stats.user_stats_id\n" +
                    "WHERE username = @username",
                    connection);

                findUserCommand.Parameters.AddWithValue("@username", username);

                await findUserCommand.PrepareAsync();
                await using var findUserReader = await findUserCommand.ExecuteReaderAsync();

                if (!await findUserReader.ReadAsync())
                {
                    return null;
                }

                var userCards = await FindUserCards(findUserReader.GetString((0)));

                return new User(
                    findUserReader.GetString(1),
                    findUserReader.GetString(2),
                    new UserData(
                        findUserReader.GetString(3),
                        findUserReader.GetString(4),
                        findUserReader.GetString(5)),
                    userCards,
                    findUserReader.GetInt32(6),
                    new UserStats(
                        findUserReader.GetInt32(7),
                        findUserReader.GetInt32(8),
                        findUserReader.GetInt32(9))
                );
            }
            catch (Exception e)
            {
                return null;
            }
        }

        private async Task<List<Card>> FindUserCards(string userId)
        {
            await using var connection = await DataSource.OpenConnectionAsync();
            await using var findUserCardsCommand = new NpgsqlCommand(
                "SELECT card.card_id, name, damage, element_type, card_type FROM user_card\n" +
                "INNER JOIN card ON card.card_id = user_card.card_id\n" +
                "WHERE user_card.user_id = @userId",
                connection);

            findUserCardsCommand.Parameters.AddWithValue("@userId", userId);

            await findUserCardsCommand.PrepareAsync();
            await using var findUserCardsReader = await findUserCardsCommand.ExecuteReaderAsync();

            var userCards = new List<Card>();

            while (await findUserCardsReader.ReadAsync())
            {
                userCards.Add(new Card(
                    findUserCardsReader.GetString(0),
                    findUserCardsReader.GetString(1),
                    findUserCardsReader.GetInt32(2),
                    Enum.Parse<ElementType>(findUserCardsReader.GetString(3)),
                    Enum.Parse<CardType>(findUserCardsReader.GetString(4))
                ));
            }

            return userCards;
        }

        public async Task<bool> ExistsByUsername(string username)
        {
            await using var connection = await DataSource.OpenConnectionAsync();
            await using var findUserExistsCommand = new NpgsqlCommand(
                "SELECT EXISTS(SELECT 1 FROM \"user\" WHERE username = @username)",
                connection);

            findUserExistsCommand.Parameters.AddWithValue("@username", username);

            await findUserExistsCommand.PrepareAsync();
            await using var findUserExistsReader = await findUserExistsCommand.ExecuteReaderAsync();

            await findUserExistsReader.ReadAsync();

            return findUserExistsReader.GetBoolean(0);
        }

        public async Task<User?> CreateUser(User user)
        {
            try
            {
                var dataId = await CreateUserData(user.UserData);
                var statsId = await CreateUserStats(user.UserStats);

                return await CreateUser(user, dataId, statsId);
            }
            catch (Exception)
            {
                return null;
            }
        }

        public async Task<bool> AuthorizeUser(string username, string password)
        {
            var user = await FindUserByUsername(username);

            if (user == null)
            {
                return false;
            }

            return user.Password == PasswordUtil.HashPassword(password);
        }

        private async Task<User?> CreateUser(User user, string userDataId, string userStatsId)
        {
            await using var connection = await DataSource.OpenConnectionAsync();
            await using var createUserCommand = new NpgsqlCommand(
                $"INSERT INTO \"user\" (user_id, username, password, user_data_id, coins, user_stats_id) " +
                $"VALUES (@userId, @username, @password, @userDataId, @coins, @userStatsId)",
                connection);

            createUserCommand.Parameters.AddWithValue("@userId", Guid.NewGuid().ToString());
            createUserCommand.Parameters.AddWithValue("@username", user.Username);
            createUserCommand.Parameters.AddWithValue("@password", PasswordUtil.HashPassword(user.Password));
            createUserCommand.Parameters.AddWithValue("@userDataId", userDataId);
            createUserCommand.Parameters.AddWithValue("@coins", user.Coins);
            createUserCommand.Parameters.AddWithValue("@userStatsId", userStatsId);

            await createUserCommand.PrepareAsync();
            await createUserCommand.ExecuteNonQueryAsync();

            return user;
        }

        private async Task<string> CreateUserData(UserData userData)
        {
            var id = Guid.NewGuid().ToString();

            await using var connection = await DataSource.OpenConnectionAsync();
            await using var createUserDataCommand = new NpgsqlCommand(
                "INSERT INTO user_data (user_data_id, name, bio, image) " +
                $"VALUES (@userDataId, @name, @bio, @image)",
                connection);

            createUserDataCommand.Parameters.AddWithValue("@userDataId", id);
            createUserDataCommand.Parameters.AddWithValue("@name", userData.Name);
            createUserDataCommand.Parameters.AddWithValue("@bio", userData.Bio);
            createUserDataCommand.Parameters.AddWithValue("@image", userData.Image);

            await createUserDataCommand.PrepareAsync();
            await createUserDataCommand.ExecuteNonQueryAsync();

            return id;
        }

        private async Task<string> CreateUserStats(UserStats userStats)
        {
            var id = Guid.NewGuid().ToString();

            await using var connection = await DataSource.OpenConnectionAsync();
            await using var createUserStatsCommand = new NpgsqlCommand(
                "INSERT INTO user_stats (user_stats_id, elo_score, wins, losses) " +
                $"VALUES (@userStatsId, @elo_score, @wins, @losses)",
                connection);

            createUserStatsCommand.Parameters.AddWithValue("@userStatsId", id);
            createUserStatsCommand.Parameters.AddWithValue("@elo_score", userStats.EloScore);
            createUserStatsCommand.Parameters.AddWithValue("@wins", userStats.Wins);
            createUserStatsCommand.Parameters.AddWithValue("@losses", userStats.Losses);

            await createUserStatsCommand.PrepareAsync();
            await createUserStatsCommand.ExecuteNonQueryAsync();

            return id;
        }

        public async Task<User?> UpdateUser(User user)
        {
            var oldUser = await FindUserByUsername(user.Username);

            if (oldUser == null)
            {
                return null;
            }

            await using var connection = await DataSource.OpenConnectionAsync();
            await using var findUserKeysCommand = new NpgsqlCommand(
                "SELECT user_id, user_data_id, user_stats_id FROM \"user\" " +
                "WHERE \"user\".username = @username",
                connection);

            findUserKeysCommand.Parameters.AddWithValue("@username", user.Username);

            await findUserKeysCommand.PrepareAsync();
            await using var findUserKeysReader = await findUserKeysCommand.ExecuteReaderAsync();

            if (!await findUserKeysReader.ReadAsync())
            {
                return null;
            }

            var userId = findUserKeysReader.GetString(0);
            var userDataId = findUserKeysReader.GetString(1);
            var userStatsId = findUserKeysReader.GetString(2);

            await findUserKeysReader.DisposeAsync();

            await using var updateUserTransaction = await connection.BeginTransactionAsync();

            try
            {
                if (!user.Coins.Equals(oldUser.Coins))
                {
                    await using var updateUserCommand = new NpgsqlCommand(
                        "UPDATE \"user\"\n" +
                        "SET coins = @newCoins\n" +
                        "WHERE username = @username",
                        connection);

                    updateUserCommand.Parameters.AddWithValue("@username", user.Username);
                    updateUserCommand.Parameters.AddWithValue("@newCoins", user.Coins);

                    await updateUserCommand.PrepareAsync();
                    await updateUserCommand.ExecuteNonQueryAsync();
                }

                if (!user.UserData.Name.Equals(oldUser.UserData.Name) ||
                    !user.UserData.Bio.Equals(oldUser.UserData.Bio) ||
                    !user.UserData.Image.Equals(oldUser.UserData.Image))
                {
                    if (await UpdateUserData(user.UserData, userDataId) == null)
                    {
                        return null;
                    }
                }

                if (!user.UserStats.EloScore.Equals(oldUser.UserStats.EloScore) ||
                    !user.UserStats.Wins.Equals(oldUser.UserStats.Wins) ||
                    !user.UserStats.Losses.Equals(oldUser.UserStats.Losses))
                {
                    if (await UpdateUserStats(user.UserStats, userStatsId) == null)
                    {
                        return null;
                    }
                }

                if (!user.Collection.Equals(oldUser.Collection))
                {
                    if (await UpdateUserCollection(user.Collection, userId) == null)
                    {
                        return null;
                    }
                }

                await updateUserTransaction.CommitAsync();

                return user;
            }
            catch (Exception)
            {
                await updateUserTransaction.RollbackAsync();
                return null;
            }
        }

        private async Task<UserData?> UpdateUserData(UserData userData, string userDataId)
        {
            await using var connection = await DataSource.OpenConnectionAsync();
            await using var updateUserDataCommand = new NpgsqlCommand(
                "UPDATE user_data\n" +
                "SET name = @newName, bio = @newBio, image = @newImage\n" +
                "WHERE user_data_id = @userDataId",
                connection);

            updateUserDataCommand.Parameters.AddWithValue("@userDataId", userDataId);
            updateUserDataCommand.Parameters.AddWithValue("@newName", userData.Name);
            updateUserDataCommand.Parameters.AddWithValue("@newBio", userData.Bio);
            updateUserDataCommand.Parameters.AddWithValue("@newImage", userData.Image);

            await updateUserDataCommand.PrepareAsync();
            await updateUserDataCommand.ExecuteNonQueryAsync();

            return userData;
        }

        private async Task<UserStats?> UpdateUserStats(UserStats userStats, string userStatsId)
        {
            await using var connection = await DataSource.OpenConnectionAsync();
            await using var updateUserStatsCommand = new NpgsqlCommand(
                "UPDATE user_stats\n" +
                "SET elo_score = @newEloScore, wins = @newWins, losses = @newLosses\n" +
                "WHERE user_stats_id = @userStatsId",
                connection);

            updateUserStatsCommand.Parameters.AddWithValue("@userStatsId", userStatsId);
            updateUserStatsCommand.Parameters.AddWithValue("@newEloScore", userStats.EloScore);
            updateUserStatsCommand.Parameters.AddWithValue("@newWins", userStats.Wins);
            updateUserStatsCommand.Parameters.AddWithValue("@newLosses", userStats.Losses);

            await updateUserStatsCommand.PrepareAsync();
            await updateUserStatsCommand.ExecuteNonQueryAsync();

            return userStats;
        }

        private async Task<List<Card>?> UpdateUserCollection(List<Card> collection, string userId)
        {
            var oldCollection = await FindUserCards(userId);

            if (oldCollection == null)
            {
                return null;
            }

            var addedCards = collection.Except(oldCollection).ToList();
            var removedCards = oldCollection.Except(collection).ToList();

            await using var connection = await DataSource.OpenConnectionAsync();
            await using var deleteRemovedUserCardsCommand = new NpgsqlCommand(
                "DELETE FROM user_card\n" +
                "WHERE user_id = @userId AND card_id = ANY(@removedCardIds)",
                connection);

            await using var addAddedUserCardsCommand = new NpgsqlCommand(
                "INSERT INTO user_card (user_id, card_id)\n" +
                "VALUES " +
                string.Join(", ",
                    Enumerable.Range(0, addedCards.Count).Select(i => $"(@userId, @cardId{i + 1})")),
                connection);

            deleteRemovedUserCardsCommand.Parameters.AddWithValue("@userId", userId);
            deleteRemovedUserCardsCommand.Parameters.AddWithValue("@removedCardIds",
                removedCards.Select(card => card.Id).ToArray());

            addAddedUserCardsCommand.Parameters.AddWithValue("@userId", userId);
            addAddedUserCardsCommand.Parameters.AddRange(addedCards
                .Select((card, i) => new NpgsqlParameter($"@cardId{i + 1}", card.Id)).ToArray());
            

            await deleteRemovedUserCardsCommand.PrepareAsync();
            await addAddedUserCardsCommand.PrepareAsync();

            await deleteRemovedUserCardsCommand.ExecuteNonQueryAsync();
            await addAddedUserCardsCommand.ExecuteNonQueryAsync();

            return collection;
        }
    }
}