using MonsterTCG.Model.Http;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.Json.Nodes;
using System.Text.Json;
using System.Threading.Tasks;
using MonsterTCG.Model;

namespace MonsterTCG.Repository
{
    public class UserRepository
    {
        private NpgsqlConnection? dbConnection;

        public UserRepository(NpgsqlConnection? dbConnection)
        {  
            this.dbConnection = dbConnection;

            NpgsqlCommand createUsersTableCommand = new NpgsqlCommand(
                @"CREATE TABLE IF NOT EXISTS User(
                Id INT CONSTRAINT id PRIMARY KEY,
                Username VARCHAR(20) NOT NULL,
                Password VARCHAR(50) NOT NULL,
                EloScore INTEGER NOT NULL
                )", this.dbConnection);

            this.dbConnection?.Open();
            createUsersTableCommand.ExecuteNonQuery();
            this.dbConnection?.Close();
        }
        public User? CreateUser(string username, string password) 
        {
            User user = new(username, password);

            try
            {
                var createUserCommand = new NpgsqlCommand(
                    @$"INSERT INTO Users (Username, Password, Eloscore)
                    VALUES ({user.Username}, {user.Password}, {user.EloScore})
                    ");

                dbConnection?.Open();
                createUserCommand.ExecuteNonQuery();
                dbConnection?.Close();

                return user;
            }
            catch (Exception e) 
            {
                return null;
            }
        }
    }
}
