using System.Net;
using MonsterTCG.Model;
using MonsterTCG.Model.Http;
using MonsterTCG.Model.User;
using MonsterTCG.Repository;
using MonsterTCG.Util;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using HttpMethod = MonsterTCG.Model.Http.HttpMethod;

namespace MonsterTCG.Controller
{
    public class UserController : Controller
    {
        private readonly IUserRepository _userRepository;

        public UserController(IUserRepository userRepository)
        {
            _userRepository = userRepository;
        }

        public override async Task<bool> ProcessRequest(HttpRequestEventArgs e)
        {
            try
            {
                var fullPath = e.Request.Path.TrimStart('/').Split('/');
                switch (fullPath)
                {
                    case ["users"] when e.Request.Method == HttpMethod.POST:
                        e.Reply(await RegisterUser(e.Request));
                        return true;
                    case ["users", var username] when e.Request.Method == HttpMethod.GET:
                        e.Reply(await GetUserData(username, e.Request));
                        return true;
                    case ["users", var username] when e.Request.Method == HttpMethod.PUT:
                        e.Reply(await UpdateUserData(username, e.Request));
                        return true;
                    case ["sessions"] when e.Request.Method == HttpMethod.POST:
                        e.Reply(await LoginUser(e.Request));
                        return true;
                    default:
                        return false;
                }
            }
            catch (Exception)
            {
                e.Reply(new HttpResponse(HttpStatusCode.InternalServerError, "An unknown error has occured"));
                return true;
            }
        }

        public async Task<HttpResponse> RegisterUser(HttpRequest request)
        {
            UserCredentials? userCredentials;

            var settings = new JsonSerializerSettings
            {
                Error = (_, args) =>
                {
                    args.ErrorContext.Handled = true;
                    userCredentials = null;
                },
                MissingMemberHandling = MissingMemberHandling.Error,
            };

            userCredentials = JsonConvert.DeserializeObject<UserCredentials>(request.Payload, settings);

            if (userCredentials is null || string.IsNullOrEmpty(userCredentials.Username) || string.IsNullOrEmpty(userCredentials.Password))
            {
                return new HttpResponse(HttpStatusCode.BadRequest, "Please provide username and password");
            }

            if (await _userRepository.ExistsByUsernameAsync(userCredentials.Username))
            {
                return new HttpResponse(HttpStatusCode.BadRequest, "User with same username already registered");
            }

            await _userRepository.CreateUserAsync(
                User.Create(
                    userCredentials.Username,
                    PasswordUtil.HashPassword(userCredentials.Password)
                )
            );

            return new HttpResponse(HttpStatusCode.Created, "User successfully created");
        }

        public async Task<HttpResponse> GetUserData(string username, HttpRequest request)
        {
            var token = request.GetBearerToken();

            if (token is null || (TokenUtil.GetUsernameFromToken(token) != "admin" &&
                                  TokenUtil.GetUsernameFromToken(token) != username))
            {
                return new HttpResponse(HttpStatusCode.Unauthorized, "Access token is missing or invalid");
            }

            var user = await _userRepository.FindByUsernameAsync(username);

            if (user is null)
            {
                return new HttpResponse(HttpStatusCode.NotFound, "User not found");
            }

            return new HttpResponse(HttpStatusCode.OK, JsonConvert.SerializeObject(user.UserData));
        }

        public async Task<HttpResponse> UpdateUserData(string username, HttpRequest request)
        {
            var token = request.GetBearerToken();

            if (token is null || (TokenUtil.GetUsernameFromToken(token) != "admin" &&
                                  TokenUtil.GetUsernameFromToken(token) != username))
            {
                return new HttpResponse(HttpStatusCode.Unauthorized,
                    "A user can only be updated by the admin or themselves");
            }

            var user = await _userRepository.FindByUsernameAsync(username);

            if (user is null)
            {
                return new HttpResponse(HttpStatusCode.NotFound, "User does not exist");
            }

            var userData = JsonConvert.DeserializeObject<UserData>(request.Payload);

            if (userData?.Name is null || userData?.Bio is null || userData?.Image is null)
            {
                return new HttpResponse(HttpStatusCode.BadRequest, "Request body missing or invalid");
            }

            user.SetUserData(userData);

            await _userRepository.UpdateUserAsync(user);

            return new HttpResponse(HttpStatusCode.OK, "User data successfully updated");
        }

        public async Task<HttpResponse> LoginUser(HttpRequest request)
        {
            UserCredentials? userCredentials;

            var settings = new JsonSerializerSettings
            {
                Error = (_, args) =>
                {
                    args.ErrorContext.Handled = true;
                    userCredentials = null;
                },
                MissingMemberHandling = MissingMemberHandling.Error,
            };

            userCredentials = JsonConvert.DeserializeObject<UserCredentials>(request.Payload, settings);

            if (userCredentials is null || string.IsNullOrEmpty(userCredentials.Username) || string.IsNullOrEmpty(userCredentials.Password))
            {
                return new HttpResponse(HttpStatusCode.BadRequest, "Username/password missing or invalid");
            }

            if (!await _userRepository.AuthorizeUserAsync(new UserCredentials(userCredentials.Username, userCredentials.Password)))
            {
                return new HttpResponse(HttpStatusCode.Unauthorized, "Invalid username/password provided");
            }

            return new HttpResponse(HttpStatusCode.OK, TokenUtil.GetTokenFromUsername(userCredentials.Username));
        }
    }
}