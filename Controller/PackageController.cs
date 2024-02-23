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
        private readonly IUserRepository _userRepository;

        public PackageController(IPackageRepository packageRepository, IUserRepository userRepository)
        {
            _packageRepository = packageRepository;
            _userRepository = userRepository;
        }

        public override Task<bool> ProcessRequest(HttpRequestEventArgs e)
        {
            try
            {
                var pathParts = e.Request.Path.TrimStart('/').Split('/');
                switch (pathParts)
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

        private async void AcquirePackage(HttpRequest request)
        {
            var token = httpRequestEventArgs.GetBearerToken();

            if (token is null)
            {
                httpRequestEventArgs.Reply(HttpStatusCode.Unauthorized, "Access token is missing or invalid");
                return;
            }

            var authenticatedUser =
                await _userRepository.FindByUsernameAsync(TokenUtil.GetUsernameFromToken(token));

            if (authenticatedUser is null)
            {
                httpRequestEventArgs.Reply(HttpStatusCode.Unauthorized, "Access token is missing or invalid");
                return;
            }

            if (authenticatedUser.Coins < 5)
            {
                httpRequestEventArgs.Reply(HttpStatusCode.Forbidden, "Not enough money for buying a card package");
                return;
            }

            var package = await _packageRepository.FindFirstPackageAsync();

            if (package is null)
            {
                httpRequestEventArgs.Reply(HttpStatusCode.NotFound, "No card package available for buying");
                return;
            }

            authenticatedUser.Collection.AddRange(package.CardList);

            authenticatedUser.SetCoins(authenticatedUser.Coins - 5);

            await _userRepository.UpdateUserAsync(authenticatedUser);

            if (!await _packageRepository.DeletePackageByIdAsync(package.PackageId))
            {
                httpRequestEventArgs.Reply(HttpStatusCode.InternalServerError, "An unknown error has occured");
                return;
            }

            httpRequestEventArgs.Reply(HttpStatusCode.OK, "A package has been successfully bought");
        }

        private async void CreatePackage(HttpRequest request)
        {
            var token = httpRequestEventArgs.GetBearerToken();

            if (token is null)
            {
                httpRequestEventArgs.Reply(HttpStatusCode.Unauthorized, "Access token is missing or invalid");
                return;
            }

            if (TokenUtil.GetUsernameFromToken(token) != "admin")
            {
                httpRequestEventArgs.Reply(HttpStatusCode.Forbidden, "Provided user is not \"admin\"");
                return;
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
                JsonConvert.DeserializeObject<List<CreateCard>>(httpRequestEventArgs.Request.Payload, settings);

            if (createCards is null || !createCards.Any())
            {
                httpRequestEventArgs.Reply(HttpStatusCode.BadRequest, "Package invalid or missing");
                return;
            }

            if (createCards.Count() != 5)
            {
                httpRequestEventArgs.Reply(HttpStatusCode.BadRequest, "Package does not contain 5 cards");
                return;
            }

            var packageCards = createCards
                .Select(createCard => Card.Create(
                        createCard.Id,
                        createCard.Name,
                        (int)createCard.Damage
                    )
                ).ToList();

            if (await _packageRepository.ExistsByCardIdsAsync(
                    (
                        packageCards[0].Id,
                        packageCards[1].Id,
                        packageCards[2].Id,
                        packageCards[3].Id,
                        packageCards[4].Id
                    )
                ))
            {
                httpRequestEventArgs.Reply(HttpStatusCode.Conflict, "Package with the same cards already created");
            }

            var package = new Package(Guid.NewGuid().ToString(),
                packageCards[0],
                packageCards[1],
                packageCards[2],
                packageCards[3],
                packageCards[4]
            );

            await _packageRepository.CreatePackageAsync(package);

            httpRequestEventArgs.Reply(HttpStatusCode.Created, "Package and missing cards successfully created");
        }
    }
}