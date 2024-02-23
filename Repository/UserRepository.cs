using System.Data;
using Npgsql;
using MonsterTCG.Model.Card;
using MonsterTCG.Model.Deck;
using MonsterTCG.Model.User;
using MonsterTCG.Util;

namespace MonsterTCG.Repository
{
    public class UserRepository : IUserRepository
    {
        private readonly NpgsqlDataSource _dataSource;
        private readonly ICardRepository _cardRepository;
        private readonly ITradeRepository _tradeRepository;

        public UserRepository(NpgsqlDataSource dataSource, ICardRepository cardRepository,
            ITradeRepository tradeRepository)
        {
            _dataSource = dataSource;
            _cardRepository = cardRepository;
            _tradeRepository = tradeRepository;
        }

        public async Task<IEnumerable<User>> AllAsync()
        {
            await using var connection = await _dataSource.OpenConnectionAsync();
            await using var getAllUsersCommand = new NpgsqlCommand(
                "SELECT user_id, username, password, name, bio, image, \"user\".deck_id, coins, elo_score, wins, losses\n" +
                "FROM \"user\"\n" +
                "INNER JOIN user_data ON \"user\".user_data_id = user_data.user_data_id\n" +
                "INNER JOIN user_stats ON \"user\".user_stats_id = user_stats.user_stats_id\n" +
                "INNER JOIN deck ON \"user\".deck_id = deck.deck_id\n",
                connection);

            await using var getAllUsersReader = await getAllUsersCommand.ExecuteReaderAsync();

            var allUsers = new List<User>();

            while (await getAllUsersReader.ReadAsync())
            {
                var userCards = (await FindUserCardsAsync(getAllUsersReader.GetString(0))).ToList();

                var userDeck = await FindDeckFromIdAsync(getAllUsersReader.GetString(6));

                var userId = getAllUsersReader.GetString("user_id");

                var userTrades = (await _tradeRepository.FindTradesByCreatorUserIdAsync(userId)).ToList();

                allUsers.Add(User.Create(
                    userId,
                    getAllUsersReader.GetString(1),
                    getAllUsersReader.GetString(2),
                    new UserData(
                        getAllUsersReader.GetString(3),
                        getAllUsersReader.GetString(4),
                        getAllUsersReader.GetString(5)),
                    userCards,
                    userDeck!,
                    getAllUsersReader.GetInt32(7),
                    new UserStats(
                        getAllUsersReader.GetInt32(8),
                        getAllUsersReader.GetInt32(9),
                        getAllUsersReader.GetInt32(10)),
                    userTrades
                ));
            }

            return allUsers;
        }

        public async Task<User?> FindByIdAsync(string id)
        {
            await using var connection = await _dataSource.OpenConnectionAsync();
            await using var findUserCommand = new NpgsqlCommand(
                "SELECT user_id, username, password, name, bio, image, \"user\".deck_id, coins, elo_score, wins, losses\n" +
                "FROM \"user\"\n" +
                "INNER JOIN user_data ON \"user\".user_data_id = user_data.user_data_id\n" +
                "INNER JOIN user_stats ON \"user\".user_stats_id = user_stats.user_stats_id\n" +
                "INNER JOIN deck ON \"user\".deck_id = deck.deck_id\n" +
                "WHERE user_id = @userId",
                connection);

            findUserCommand.Parameters.AddWithValue("@userId", id);

            await findUserCommand.PrepareAsync();
            await using var findUserReader = await findUserCommand.ExecuteReaderAsync();

            if (!await findUserReader.ReadAsync())
            {
                return null;
            }

            var userCards = await FindUserCardsAsync(findUserReader.GetString(0));

            var userDeck = await FindDeckFromIdAsync(findUserReader.GetString(6));

            var userId = findUserReader.GetString("user_id");

            var userTrades = (await _tradeRepository.FindTradesByCreatorUserIdAsync(userId)).ToList();

            return User.Create(
                findUserReader.GetString(0),
                findUserReader.GetString(1),
                findUserReader.GetString(2),
                new UserData(
                    findUserReader.GetString(3),
                    findUserReader.GetString(4),
                    findUserReader.GetString(5)),
                userCards.ToList(),
                userDeck!,
                findUserReader.GetInt32(7),
                new UserStats(
                    findUserReader.GetInt32(8),
                    findUserReader.GetInt32(9),
                    findUserReader.GetInt32(10)),
                userTrades
            );
        }

        /// <summary>
        /// Finds a user by their username
        /// </summary>
        /// <param name="username">The username of the user</param>
        /// <returns>The user with the given username. If no such user has been found, returns null</returns>
        public async Task<User?> FindByUsernameAsync(string username)
        {
            await using var connection = await _dataSource.OpenConnectionAsync();
            await using var findUserCommand = new NpgsqlCommand(
                "SELECT user_id, username, password, name, bio, image, \"user\".deck_id, coins, elo_score, wins, losses\n" +
                "FROM \"user\"\n" +
                "INNER JOIN user_data ON \"user\".user_data_id = user_data.user_data_id\n" +
                "INNER JOIN user_stats ON \"user\".user_stats_id = user_stats.user_stats_id\n" +
                "INNER JOIN deck ON \"user\".deck_id = deck.deck_id\n" +
                "WHERE username = @username",
                connection);

            findUserCommand.Parameters.AddWithValue("@username", username);

            await findUserCommand.PrepareAsync();
            await using var findUserReader = await findUserCommand.ExecuteReaderAsync();

            if (!await findUserReader.ReadAsync())
            {
                return null;
            }

            var userCards = await FindUserCardsAsync(findUserReader.GetString(0));

            var userDeck = await FindDeckFromIdAsync(findUserReader.GetString(6));

            var userId = findUserReader.GetString("user_id");

            var userTrades = (await _tradeRepository.FindTradesByCreatorUserIdAsync(userId)).ToList();

            return User.Create(
                findUserReader.GetString(0),
                findUserReader.GetString(1),
                findUserReader.GetString(2),
                new UserData(
                    findUserReader.GetString(3),
                    findUserReader.GetString(4),
                    findUserReader.GetString(5)),
                userCards.ToList(),
                userDeck!,
                findUserReader.GetInt32(7),
                new UserStats(
                    findUserReader.GetInt32(8),
                    findUserReader.GetInt32(9),
                    findUserReader.GetInt32(10)),
                userTrades
            );
        }

        private async Task<IEnumerable<Card>> FindUserCardsAsync(string userId)
        {
            await using var connection = await _dataSource.OpenConnectionAsync();
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
                userCards.Add(Card.Create(
                    findUserCardsReader.GetString(0),
                    findUserCardsReader.GetString(1),
                    findUserCardsReader.GetInt32(2),
                    Enum.Parse<ElementType>(findUserCardsReader.GetString(3)),
                    Enum.Parse<CardType>(findUserCardsReader.GetString(4))
                ));
            }

            return userCards;
        }

        public async Task<Deck?> FindDeckFromIdAsync(string deckId)
        {
            await using var connection = await _dataSource.OpenConnectionAsync();
            await using var findDeckCommand = new NpgsqlCommand(
                "SELECT * FROM deck\n" +
                "WHERE deck_id = @deckId",
                connection);

            findDeckCommand.Parameters.AddWithValue("@deckId", deckId);

            await findDeckCommand.PrepareAsync();
            await using var findDeckReader = await findDeckCommand.ExecuteReaderAsync();

            if (!await findDeckReader.ReadAsync())
            {
                return null;
            }

            var deckCardIds = new List<string>
            {
                findDeckReader.GetString(1),
                findDeckReader.GetString(2),
                findDeckReader.GetString(3),
                findDeckReader.GetString(4),
            };

            await findDeckReader.DisposeAsync();

            var deckCards = (await _cardRepository.FindCardsByIdsAsync(deckCardIds))?.ToList();

            return deckCards is null ? null :
                deckCards.Count > 0 ? new Deck(deckId, (deckCards[0], deckCards[1], deckCards[2], deckCards[3])) :
                new Deck(deckId);
        }

        private async Task<UserCredentials?> FindUserCredentialsAsync(string username)
        {
            await using var connection = await _dataSource.OpenConnectionAsync();
            await using var findUserCredentialsCommand = new NpgsqlCommand(
                "SELECT username, password FROM \"user\"\n" +
                "WHERE username = @username",
                connection);

            findUserCredentialsCommand.Parameters.AddWithValue("@username", username);

            await findUserCredentialsCommand.PrepareAsync();
            await using var findUserCredentialsReader = await findUserCredentialsCommand.ExecuteReaderAsync();

            if (!await findUserCredentialsReader.ReadAsync())
            {
                return null;
            }

            return new UserCredentials(
                findUserCredentialsReader.GetString("username"),
                findUserCredentialsReader.GetString("password")
            );
        }

        public async Task<bool> ExistsByUsernameAsync(string username)
        {
            await using var connection = await _dataSource.OpenConnectionAsync();
            await using var findUserExistsCommand = new NpgsqlCommand(
                "SELECT EXISTS(SELECT 1 FROM \"user\" WHERE username = @username)",
                connection);

            findUserExistsCommand.Parameters.AddWithValue("@username", username);

            await findUserExistsCommand.PrepareAsync();
            await using var findUserExistsReader = await findUserExistsCommand.ExecuteReaderAsync();

            await findUserExistsReader.ReadAsync();

            return findUserExistsReader.GetBoolean(0);
        }

        public async Task<bool> HasCardFromIdAsync(User user, string cardId)
        {
            await using var connection = await _dataSource.OpenConnectionAsync();
            await using var findUserCardsCommand = new NpgsqlCommand(
                "SELECT card_id FROM user_card\n" +
                "WHERE user_id = @userId AND card_id = @cardId",
                connection);

            findUserCardsCommand.Parameters.AddWithValue("@userId", user.Id);
            findUserCardsCommand.Parameters.AddWithValue("@cardId", cardId);

            await findUserCardsCommand.PrepareAsync();
            await using var findUserCardsReader = await findUserCardsCommand.ExecuteReaderAsync();

            return await findUserCardsReader.ReadAsync();
        }

        public async Task<bool> HasCardsFromIdsAsync(User user, List<string> cardIds)
        {
            await using var connection = await _dataSource.OpenConnectionAsync();
            await using var findUserCardsCommand = new NpgsqlCommand(
                "SELECT card_id FROM user_card\n" +
                "WHERE user_id = @userId AND card_id = ANY(@cardIds)",
                connection);

            findUserCardsCommand.Parameters.AddWithValue("@userId", user.Id);
            findUserCardsCommand.Parameters.AddWithValue("@cardIds", cardIds);

            await findUserCardsCommand.PrepareAsync();
            await using var findUserCardsReader = await findUserCardsCommand.ExecuteReaderAsync();

            var rowCount = 0;

            while (await findUserCardsReader.ReadAsync())
            {
                rowCount++;
            }

            return rowCount == cardIds.Count;
        }

        public async Task<bool> AuthorizeUserAsync(string username, string password)
        {
            var credentials = await FindUserCredentialsAsync(username);

            return credentials is not null && credentials.Password == PasswordUtil.HashPassword(password);
        }

        public async Task CreateUserAsync(User user)
        {
            await using var connection = await _dataSource.OpenConnectionAsync();
            await using var createUserTransaction = await connection.BeginTransactionAsync();

            try
            {
                var dataId = await CreateUserDataAsync(user.UserData, connection);
                var statsId = await CreateUserStatsAsync(user.UserStats, connection);

                await CreateDeckAsync(user.Deck, connection);

                await CreateUserAsync(user, dataId, statsId, connection);

                await createUserTransaction.CommitAsync();
            }
            catch (Exception)
            {
                await createUserTransaction.RollbackAsync();
            }
        }

        private async Task<User?> CreateUserAsync(User user, string userDataId, string userStatsId,
            NpgsqlConnection connection)
        {
            await using var createUserCommand = new NpgsqlCommand(
                $"INSERT INTO \"user\" (user_id, username, password, user_data_id, deck_id, coins, user_stats_id) " +
                $"VALUES (@userId, @username, @password, @userDataId, @deckId, @coins, @userStatsId)",
                connection);

            createUserCommand.Parameters.AddWithValue("@userId", Guid.NewGuid().ToString());
            createUserCommand.Parameters.AddWithValue("@username", user.Username);
            createUserCommand.Parameters.AddWithValue("@password", user.Password);
            createUserCommand.Parameters.AddWithValue("@userDataId", userDataId);
            createUserCommand.Parameters.AddWithValue("@deckId", user.Deck.Id);
            createUserCommand.Parameters.AddWithValue("@coins", user.Coins);
            createUserCommand.Parameters.AddWithValue("@userStatsId", userStatsId);

            await createUserCommand.PrepareAsync();
            await createUserCommand.ExecuteNonQueryAsync();

            return user;
        }

        private async Task<string> CreateUserDataAsync(UserData userData, NpgsqlConnection connection)
        {
            var id = Guid.NewGuid().ToString();

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

        private async Task<string> CreateUserStatsAsync(UserStats userStats, NpgsqlConnection connection)
        {
            var id = Guid.NewGuid().ToString();

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

        private async Task<Deck> CreateDeckAsync(Deck deck, NpgsqlConnection connection)
        {
            await using var createDeckCommand = new NpgsqlCommand(
                "INSERT INTO deck (deck_id, card_id_1, card_id_2, card_id_3, card_id_4) " +
                "VALUES (@deckId, '', '', '', '')",
                connection);

            createDeckCommand.Parameters.AddWithValue("@deckId", deck.Id);

            await createDeckCommand.PrepareAsync();
            await createDeckCommand.ExecuteNonQueryAsync();

            return deck;
        }

        public async Task UpdateUserAsync(User user)
        {
            var oldUser = await FindByUsernameAsync(user.Username);

            if (oldUser is null)
            {
                throw new ArgumentException("User not found");
            }

            await using var connection = await _dataSource.OpenConnectionAsync();
            await using var findUserKeysCommand = new NpgsqlCommand(
                "SELECT user_id, user_data_id, user_stats_id FROM \"user\" " +
                "WHERE \"user\".user_id = @userId",
                connection);

            findUserKeysCommand.Parameters.AddWithValue("@userId", user.Id);

            await findUserKeysCommand.PrepareAsync();
            await using var findUserKeysReader = await findUserKeysCommand.ExecuteReaderAsync();

            await findUserKeysReader.ReadAsync();

            var userId = findUserKeysReader.GetString("user_id");
            var userDataId = findUserKeysReader.GetString("user_data_id");
            var userStatsId = findUserKeysReader.GetString("user_stats_id");

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
                    await UpdateUserDataAsync(user.UserData, userDataId, connection);
                }

                if (!user.UserStats.EloScore.Equals(oldUser.UserStats.EloScore) ||
                    !user.UserStats.Wins.Equals(oldUser.UserStats.Wins) ||
                    !user.UserStats.Losses.Equals(oldUser.UserStats.Losses))
                {
                    await UpdateUserStatsAsync(user.UserStats, userStatsId, connection);
                }

                if (!user.Deck.Equals(oldUser.Deck))
                {
                    await UpdateDeckAsync(user.Deck, connection);
                }

                if (!user.Collection.SequenceEqual(oldUser.Collection))
                {
                    await UpdateUserCollectionAsync(user.Collection, userId, connection);
                }

                await updateUserTransaction.CommitAsync();
            }
            catch (Exception)
            {
                await updateUserTransaction.RollbackAsync();
            }
        }

        private async Task<UserData?> UpdateUserDataAsync(UserData userData, string userDataId,
            NpgsqlConnection connection)
        {
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

        private async Task<UserStats?> UpdateUserStatsAsync(UserStats userStats, string userStatsId,
            NpgsqlConnection connection)
        {
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

        private async Task<Deck> UpdateDeckAsync(Deck deck, NpgsqlConnection connection)
        {
            await using var updateDeckCommand = new NpgsqlCommand(
                "UPDATE deck\n" +
                $"SET card_id_1 = @cardId1, card_id_2 = @cardId2, card_id_3 = @cardId3, card_id_4 = @cardId4\n" +
                "WHERE deck_id = @deckId",
                connection);

            updateDeckCommand.Parameters.AddWithValue("@deckId", deck.Id);
            updateDeckCommand.Parameters.AddWithValue("@cardId1", deck.Cards.card1?.Id ?? "");
            updateDeckCommand.Parameters.AddWithValue("@cardId2", deck.Cards.card2?.Id ?? "");
            updateDeckCommand.Parameters.AddWithValue("@cardId3", deck.Cards.card3?.Id ?? "");
            updateDeckCommand.Parameters.AddWithValue("@cardId4", deck.Cards.card4?.Id ?? "");

            await updateDeckCommand.PrepareAsync();
            await updateDeckCommand.ExecuteNonQueryAsync();

            return deck;
        }

        private async Task<List<Card>?> UpdateUserCollectionAsync(List<Card> collection, string userId,
            NpgsqlConnection connection)
        {
            var oldCollection = await FindUserCardsAsync(userId);

            var addedCards = collection.Except(oldCollection).ToList();
            var removedCards = oldCollection.Except(collection).ToList();

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