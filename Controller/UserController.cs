using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.Json.Nodes;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using MonsterTCG.Model.Http;
using Npgsql;
using MonsterTCG.Repository;
using MonsterTCG.Controller.Interface;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;

namespace MonsterTCG.Controller
{
    public class UserController : IHttpController
    {
        private HttpServer httpServer;
        private UserRepository userRepository;

        public UserController(HttpServer httpServer, UserRepository userRepository)
        {
            this.httpServer = httpServer;
            this.userRepository = userRepository;

            this.httpServer.IncomingRequest += _ProcessRequest;
        }

        public void _ProcessRequest(object sender, HttpRequestEventArgs e)
        {
            HttpRequest request = e.Request;

            switch(request?.Path)
            {
                case "/users":
                    JObject payloadObject = (JObject)JsonConvert.DeserializeObject(request.Payload);
#
                    payloadObject.TryGetPropertyValue<string>("Username", out string username);

                    payloadObject.TryGetPropertyValue<string>("Password", out var password);

                    userRepository.CreateUser(username, password?.ToString());
                    break;
                case var path when new Regex().IsMatch(path):

                    break;
                default:
                    e.Reply(HttpStatusCode.NotFound); 
                    break;
            }
        }

        private void CreateUser(HttpRequestEventArgs httpRequestEventArgs)
        {
            JsonObject userData = JsonSerializer.Deserialize<JsonObject>(httpRequestEventArgs.Request.Payload);

            try
            {
                userData.TryGetPropertyValue("Username", out var username);
                userData.TryGetPropertyValue("Password", out var password);

                var createUserCommand = new NpgsqlCommand(
                    @$"INSERT INTO Users (Username, Password, Eloscore)
                    VALUES ({username}, {password}, 500)
                    ");

                _dbConnection?.Open();
                createUserCommand.ExecuteNonQuery();
                _dbConnection?.Close();
            }
            catch (Exception e)
            {
                return new HttpResponse() { StatusCode = HttpStatusCode.BadRequest, Message = "Request body is invalid." };
            }

            return new HttpResponse() { StatusCode = HttpStatusCode.Created, Message = "User successfully created" };
        }
    }
}
