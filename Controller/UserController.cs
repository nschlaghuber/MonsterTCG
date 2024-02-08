using System.Net;
using MonsterTCG.Model;
using MonsterTCG.Model.Http;
using MonsterTCG.Repository;
using MonsterTCG.Util;
using Newtonsoft.Json.Linq;
using HttpMethod = MonsterTCG.Model.Http.HttpMethod;

namespace MonsterTCG.Controller
{
    public class UserController : Controller
    {
        private readonly UserRepository _userRepository;

        public UserController(UserRepository userRepository)
        {
            _userRepository = userRepository;
        }

        public override bool ProcessRequest(HttpRequestEventArgs e)
        {
            try
            {
                var test = e.Request.Path.TrimStart('/').Split('/');
                switch (test)
                {
                    case ["users"] when e.Request.Method == HttpMethod.POST:
                        RegisterUser(e);
                        return true;
                    case ["users", _] parts when e.Request.Method == HttpMethod.GET:
                        GetUser(parts[1], e);
                        return true;
                    case ["sessions"] when e.Request.Method == HttpMethod.POST:
                        LoginUser(e);
                        return true;
                    default:
                        return false;
                }
            }
            catch (Exception)
            {
                e.Reply(HttpStatusCode.InternalServerError, "An unknown error has occured");
                return true;
            }
        }

        public async void RegisterUser(HttpRequestEventArgs httpRequestEventArgs)
        {
            var payloadObject = JObject.Parse(httpRequestEventArgs.Request.Payload);

            var username = (string?)payloadObject["Username"];
            var password = (string?)payloadObject["Password"];

            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            {
                httpRequestEventArgs.Reply(HttpStatusCode.BadRequest, "Please provide username and password");
                return;
            }

            if (await _userRepository.ExistsByUsername(username))
            {
                httpRequestEventArgs.Reply(HttpStatusCode.BadRequest, "User with same username already registered");
                return;
            }

            if(await _userRepository.CreateUser(new User(username, password)) is null)
            {
                httpRequestEventArgs.Reply(HttpStatusCode.InternalServerError, "An unknown error has occured");
                return;
            }

            httpRequestEventArgs.Reply(HttpStatusCode.Created, "User successfully created");
        }

        public async void GetUser(string username, HttpRequestEventArgs httpRequestEventArgs)
        {
            var token = httpRequestEventArgs.GetBearerToken();

            if (token == null || TokenUtil.GetUsernameFromToken(token) != "admin" || TokenUtil.GetUsernameFromToken(token) != username)
            {
                httpRequestEventArgs.Reply(HttpStatusCode.Unauthorized, "Access token is missing or invalid");
                return;
            }

            var user = await _userRepository.FindUserByUsername(username);

            if (user == null)
            {
                httpRequestEventArgs.Reply(HttpStatusCode.NotFound, "User not found");
            }

            httpRequestEventArgs.Reply(HttpStatusCode.OK, "Data successfully retrieved");
        }

        public async void UpdateUser(HttpRequestEventArgs httpRequestEventArgs)
        {
            var payloadObject = JObject.Parse(httpRequestEventArgs.Request.Payload);



            throw new NotImplementedException();
        }

        public async void LoginUser(HttpRequestEventArgs httpRequestEventArgs)
        {
            var payloadObject = JObject.Parse(httpRequestEventArgs.Request.Payload);

            if (!payloadObject.TryGetValue("Username", out JToken? usernameJToken))
            {
                httpRequestEventArgs.Reply(HttpStatusCode.Unauthorized, "Invalid username/password provided");
                return;
            }
            if (!payloadObject.TryGetValue("Password", out JToken? passwordJToken))
            {
                httpRequestEventArgs.Reply(HttpStatusCode.Unauthorized, "Invalid username/password provided");
                return;
            }

            var username = (string?)usernameJToken;
            var password = (string?)passwordJToken;

            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            {
                httpRequestEventArgs.Reply(HttpStatusCode.Unauthorized, "Invalid username/password provided");
                return;
            }

            if (!await _userRepository.AuthorizeUser(username, password))
            {
                httpRequestEventArgs.Reply(HttpStatusCode.Unauthorized, "Invalid username/password provided");
                return;
            }

            httpRequestEventArgs.Reply(HttpStatusCode.OK, "User login successful\n" + TokenUtil.GetTokenFromUsername(username));
        }
    }
}
