using MonsterTCG.Model.Http;
using MonsterTCG.Repository;
using MonsterTCG.Util;
using Newtonsoft.Json.Linq;
using System.Net;
using MonsterTCG.Model.Card;
using MonsterTCG.Model.Package;
using MonsterTCG.Model.User;
using Newtonsoft.Json;
using HttpMethod = MonsterTCG.Model.Http.HttpMethod;

namespace MonsterTCG.Controller
{
    public class PackageController : Controller
    {
        private readonly IPackageRepository _packageRepository;
        private readonly ICardRepository _cardRepository;
        private readonly IUserRepository _userRepository;

        public PackageController(IPackageRepository packageRepository, ICardRepository cardRepository,
            IUserRepository userRepository)
        {
            _packageRepository = packageRepository;
            _cardRepository = cardRepository;
            _userRepository = userRepository;
        }

        public override async Task<bool> ProcessRequest(HttpRequestEventArgs e)
        {
            try
            {
                var pathParts = e.Request.Path.TrimStart('/').Split('/');

                HttpResponse? response;
                switch (pathParts)
                {
                    case ["packages"] when e.Request.Method == HttpMethod.POST:
                        e.Reply(await CreatePackage(e.Request));
                        return true;
                    case ["transactions", "packages"] when e.Request.Method == HttpMethod.POST:
                        e.Reply(await AcquirePackage(e.Request));
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

        public async Task<HttpResponse> AcquirePackage(HttpRequest request)
        {
            var token = request.GetBearerToken();

            if (token is null)
            {
                return new HttpResponse(HttpStatusCode.Unauthorized, "Access token is missing or invalid");
            }

            var authenticatedUser =
                await _userRepository.FindByUsernameAsync(TokenUtil.GetUsernameFromToken(token));

            if (authenticatedUser is null)
            {
                return new HttpResponse(HttpStatusCode.Unauthorized, "Access token is missing or invalid");
            }

            if (authenticatedUser.Coins < 5)
            {
                return new HttpResponse(HttpStatusCode.Forbidden, "Not enough money for buying a card package");
            }

            var package = await _packageRepository.FindFirstPackageAsync();

            if (package is null)
            {
                return new HttpResponse(HttpStatusCode.NotFound, "No card package available for buying");
            }

            authenticatedUser.Collection.AddRange(package.CardList);

            authenticatedUser.SetCoins(authenticatedUser.Coins - 5);

            if (!await _packageRepository.DeletePackageByIdAsync(package.PackageId))
            {
                return new HttpResponse(HttpStatusCode.InternalServerError, "An unknown error has occured");
            }

            await _userRepository.UpdateUserAsync(authenticatedUser);

            return new HttpResponse(HttpStatusCode.OK, "A package has been successfully bought");
        }

        public async Task<HttpResponse> CreatePackage(HttpRequest request)
        {
            var token = request.GetBearerToken();

            if (token is null)
            {
                return new HttpResponse(HttpStatusCode.Unauthorized, "Access token is missing or invalid");
            }

            if (TokenUtil.GetUsernameFromToken(token) != "admin")
            {
                return new HttpResponse(HttpStatusCode.Forbidden, "Provided user is not \"admin\"");
            }

            IEnumerable<CreateCard>? createCards;

            var settings = new JsonSerializerSettings
            {
                Error = (_, args) =>
                {
                    args.ErrorContext.Handled = true;
                    createCards = null;
                },
                MissingMemberHandling = MissingMemberHandling.Error,
            };

            createCards =
                JsonConvert.DeserializeObject<List<CreateCard>>(request.Payload, settings);

            if (createCards is null || !createCards.Any())
            {
                return new HttpResponse(HttpStatusCode.BadRequest, "Package invalid or missing");
            }

            if (createCards.Count() != 5)
            {
                return new HttpResponse(HttpStatusCode.BadRequest, "Package does not contain 5 cards");
            }

            var packageCards = createCards
                .Select(createCard => Card.Create(
                        createCard.Id,
                        createCard.Name,
                        (int)createCard.Damage
                    )
                ).ToList();

            if (await _cardRepository.ExistAnyByIdsAsync(new List<string>
                    {
                        packageCards[0].Id,
                        packageCards[1].Id,
                        packageCards[2].Id,
                        packageCards[3].Id,
                        packageCards[4].Id
                    }
                ))
            {
                return new HttpResponse(HttpStatusCode.Conflict, "At least one card in the packages already exists");
            }

            var package = Package.Create(Guid.NewGuid().ToString(),
                packageCards[0],
                packageCards[1],
                packageCards[2],
                packageCards[3],
                packageCards[4]
            );

            await _packageRepository.CreatePackageAsync(package);

            return new HttpResponse(HttpStatusCode.Created, "Package and missing cards successfully created");
        }
    }
}