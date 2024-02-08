using System.Diagnostics.CodeAnalysis;
using MonsterTCG.Model;
using MonsterTCG.Model.Http;
using MonsterTCG.Repository;
using MonsterTCG.Util;
using Newtonsoft.Json.Linq;
using System.Net;
using System.Net.Cache;
using HttpMethod = MonsterTCG.Model.Http.HttpMethod;

namespace MonsterTCG.Controller
{
    public class PackageController : Controller
    {
        private readonly PackageRepository _packageRepository;
        private readonly CardRepository _cardRepository;
        private readonly UserRepository _userRepository;

        public PackageController(PackageRepository packageRepository, CardRepository cardRepository, UserRepository userRepository)
        {
            _packageRepository = packageRepository;
            _cardRepository = cardRepository;
            _userRepository = userRepository;
        }

        public override bool ProcessRequest(HttpRequestEventArgs e)
        {
            try
            {
                var test = e.Request.Path.TrimStart('/').Split('/');
                switch (test)
                {
                    case ["packages"] when e.Request.Method == HttpMethod.POST:
                        CreatePackage(e);
                        return true;
                    case ["transactions", "packages"] when e.Request.Method == HttpMethod.POST:
                        AcquirePackage(e);
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

        private async void AcquirePackage(HttpRequestEventArgs httpRequestEventArgs)
        {
            
            var token = httpRequestEventArgs.GetBearerToken();

            if (token == null)
            {
                httpRequestEventArgs.Reply(HttpStatusCode.Unauthorized, "Access token is missing or invalid");
                return;
            }

            var authenticatedUser = await _userRepository.FindUserByUsername(TokenUtil.GetUsernameFromToken(token));

            if (authenticatedUser == null)
            {
                httpRequestEventArgs.Reply(HttpStatusCode.Unauthorized, "Access token is missing or invalid");
                return;
            }

            if (authenticatedUser.Coins < 5)
            {
                httpRequestEventArgs.Reply(HttpStatusCode.Forbidden, "Not enough money for buying a card package");
                return;
            }

            var package = await _packageRepository.FindFirstPackage();

            if (package == null)
            {
                httpRequestEventArgs.Reply(HttpStatusCode.NotFound, "No card package available for buying");
                return;
            }
            
            authenticatedUser.Collection.AddRange(package.CardList);
            
            authenticatedUser.Coins -= 5;

            if (await _userRepository.UpdateUser(authenticatedUser) == null)
            {
                httpRequestEventArgs.Reply(HttpStatusCode.InternalServerError, "An unknown error has occured");
                return;
            }

            if (!await _packageRepository.DeletePackageById(package.PackageId))
            {
                httpRequestEventArgs.Reply(HttpStatusCode.InternalServerError, "An unknown error has occured");
                return;
            }
            
            httpRequestEventArgs.Reply(HttpStatusCode.OK, "A package has been successfully bought");
        }

        private async void CreatePackage(HttpRequestEventArgs httpRequestEventArgs)
        {
            var token = httpRequestEventArgs.GetBearerToken();
            
            if (token == null)
            {
                httpRequestEventArgs.Reply(HttpStatusCode.Unauthorized, "Access token is missing or invalid");
                return;
            }

            if (TokenUtil.GetUsernameFromToken(token) != "admin")
            {
                httpRequestEventArgs.Reply(HttpStatusCode.Forbidden, "Provided user is not \"admin\"");
                return;
            }

            var payloadObjectArray = JArray.Parse(httpRequestEventArgs.Request.Payload);

            if (payloadObjectArray.Count != 5)
            {
                httpRequestEventArgs.Reply(HttpStatusCode.BadRequest, "A package needs to contain exactly 5 cards");
                return;
            }

            var cards = new List<Card>();

            foreach (var jToken in payloadObjectArray)
            {
                var cardJObject = (JObject)jToken;

                if (!cardJObject.TryGetValue("Id", out var id) ||
                    !cardJObject.TryGetValue("Name", out var name) ||
                    !cardJObject.TryGetValue("Damage", out var damage))
                {
                    httpRequestEventArgs.Reply(HttpStatusCode.BadRequest, "Card data is missing/invalid");
                    return;
                }

                var elementType =
                    name.ToString().Contains("Fire") ? ElementType.Fire :
                    name.ToString().Contains("Water") ? ElementType.Water :
                    ElementType.Normal;

                var cardType =
                    name.ToString().Contains("Spell") ? CardType.Spell : CardType.Monster;

                cards.Add(new Card(id.ToString(), name.ToString(), (int)damage, elementType, cardType));
            }

            if (await _packageRepository.ExistsByCardIds((cards[0]?.Id, cards[1]?.Id, cards[2]?.Id, cards[3]?.Id,
                    cards[4]?.Id)))
            {
                httpRequestEventArgs.Reply(HttpStatusCode.Conflict, "Package with the same cards already created");
            }

            var package = new Package(Guid.NewGuid().ToString(), cards[0], cards[1], cards[2], cards[3], cards[4]);

            if (await _packageRepository.CreatePackage(package) != null)
            {
                httpRequestEventArgs.Reply(HttpStatusCode.Created, "Package and missing cards successfully created");
                return;
            }

            httpRequestEventArgs.Reply(HttpStatusCode.InternalServerError, "An unknown error has occured");
        }
    }
}