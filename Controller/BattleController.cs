using System.Net;
using MonsterTCG.Model.Battle;
using MonsterTCG.Model.Card;
using MonsterTCG.Model.Deck;
using MonsterTCG.Model.Http;
using MonsterTCG.Model.User;
using MonsterTCG.Repository;
using MonsterTCG.Service.BattleService;
using MonsterTCG.Util;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using HttpMethod = MonsterTCG.Model.Http.HttpMethod;

namespace MonsterTCG.Controller;

public class BattleController : Controller
{
    private readonly IBattleRepository _battleRepository;
    private readonly IUserRepository _userRepository;
    private readonly IBattleService _battleService;

    public BattleController(IBattleRepository battleRepository, IUserRepository userRepository,
        IBattleService battleService)
    {
        _battleRepository = battleRepository;
        _userRepository = userRepository;
        _battleService = battleService;
    }

    public override async Task<bool> ProcessRequest(HttpRequestEventArgs e)
    {
        try
        {
            var fullPath = e.Request.Path.TrimStart('/').Split('/');

            switch (fullPath)
            {
                case ["stats"] when e.Request.Method == HttpMethod.GET:
                    e.Reply(await GetStats(e.Request));
                    return true;
                case ["scoreboard"] when e.Request.Method == HttpMethod.GET:
                    e.Reply(await GetScoreboard(e.Request));
                    return true;
                case ["battles"] when e.Request.Method == HttpMethod.POST:
                    await DoBattle(e.Request, e.Reply);
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

    public async Task<HttpResponse> GetStats(HttpRequest request)
    {
        var token = request.GetBearerToken();

        if (token is null)
        {
            return new HttpResponse(HttpStatusCode.Unauthorized, "Access token is missing or invalid");
        }

        var authenticatedUser = await _userRepository.FindByUsernameAsync(TokenUtil.GetUsernameFromToken(token));

        if (authenticatedUser is null)
        {
            return new HttpResponse(HttpStatusCode.Unauthorized, "Access token is missing or invalid");
        }

        return new HttpResponse(HttpStatusCode.OK,
            JsonConvert.SerializeObject(
                new GetUserStats(
                    authenticatedUser.Username,
                    authenticatedUser.UserStats.EloScore,
                    authenticatedUser.UserStats.Wins,
                    authenticatedUser.UserStats.Losses),
                Formatting.Indented)
        );
    }

    public async Task<HttpResponse> GetScoreboard(HttpRequest request)
    {
        var token = request.GetBearerToken();

        if (token is null)
        {
            return new HttpResponse(HttpStatusCode.Unauthorized, "Access token is missing or invalid");
        }

        var authenticatedUser = await _userRepository.FindByUsernameAsync(TokenUtil.GetUsernameFromToken(token));

        if (authenticatedUser is null)
        {
            return new HttpResponse(HttpStatusCode.Unauthorized, "Access token is missing or invalid");
        }

        var scoreboard = (await _userRepository.AllAsync())
            .Select(user => new ScoreboardItem(
                !string.IsNullOrEmpty(user.UserData.Name) ? user.UserData.Name : "No name",
                user.UserStats.EloScore,
                user.UserStats.Wins,
                user.UserStats.Losses)
            ).OrderBy(item => -item.Elo).ToList();

        return new HttpResponse(HttpStatusCode.OK, JsonConvert.SerializeObject(scoreboard));
    }

    public async Task DoBattle(HttpRequest request, Action<HttpResponse> onFinished)
    {
        var token = request.GetBearerToken();

        if (token is null)
        {
            onFinished(new HttpResponse(HttpStatusCode.Unauthorized, "Access token is missing or invalid"));
            return;
        }

        var authenticatedUser = await _userRepository.FindByUsernameAsync(TokenUtil.GetUsernameFromToken(token));

        if (authenticatedUser is null)
        {
            onFinished(new HttpResponse(HttpStatusCode.Unauthorized, "Access token is missing or invalid"));
            return;
        }

        if (!TupleUtil.GetListFromTuple<Card>(authenticatedUser.Deck.Cards).Any(card => card is not null))
        {
            onFinished(new HttpResponse(HttpStatusCode.BadRequest, "User has not configured their deck"));
            return;
        }

        if (string.IsNullOrEmpty(request.Payload) ||
            !JObject.Parse(request.Payload).TryGetValue("Bet", out var betJToken))
        {
            onFinished(new HttpResponse(HttpStatusCode.BadRequest, "No bet placed"));
            return;
        }

        if (!Enum.TryParse<Bet>(betJToken!.ToString(), out var bet))
        {
            onFinished(new HttpResponse(HttpStatusCode.BadRequest,
                "Possible bets are: None (0), Small (5), Medium (10), Large (20), Huge (50), AllIn"));
            return;
        }

        var battleRequest = new BattleRequest(authenticatedUser, bet);

        battleRequest.BattleFinished += async (_, args) =>
        {
            authenticatedUser.SetCoins(Math.Max(authenticatedUser.Coins + args.AcquiredCoins, 0));
            authenticatedUser.AddEloScore(args.AcquiredEloScore);

            switch (args.Outcome)
            {
                case Outcome.Victory:
                    authenticatedUser.AddWin();
                    break;
                case Outcome.Defeat:
                    authenticatedUser.AddLoss();
                    break;
            }

            await _userRepository.UpdateUserAsync(authenticatedUser);

            onFinished(new HttpResponse(HttpStatusCode.OK,
                $"You {args.Outcome switch {
                    Outcome.Victory => "won!",
                    Outcome.Defeat => "lost...",
                    Outcome.Draw => "drew.",
                    _ => "'ve unlocked a secret outcome" }
                }\n\nHere's the battle log:\n\n=======================================\n\n" + args.BattleLog,
                FormatType.Plain)
            );
        };

        _battleService.QueueForBattle(battleRequest);
    }
}