using System.Net;
using MonsterTCG.Model.Card;
using MonsterTCG.Model.Http;
using MonsterTCG.Model.Trade;
using MonsterTCG.Repository;
using MonsterTCG.Util;
using Newtonsoft.Json;
using HttpMethod = MonsterTCG.Model.Http.HttpMethod;

namespace MonsterTCG.Controller;

public class TradeController : Controller
{
    private readonly ITradeRepository _tradeRepository;
    private readonly IUserRepository _userRepository;
    private readonly ICardRepository _cardRepository;

    public TradeController(ITradeRepository tradeRepository, IUserRepository userRepository,
        ICardRepository cardRepository)
    {
        _tradeRepository = tradeRepository;
        _userRepository = userRepository;
        _cardRepository = cardRepository;
    }

    public override async Task<bool> ProcessRequest(HttpRequestEventArgs e)
    {
        try
        {
            var pathParts = e.Request.Path.TrimStart('/').Split('/');
            switch (pathParts)
            {
                case ["tradings"] when e.Request.Method == HttpMethod.GET:
                    GetAllTrades(e);
                    return true;
                case ["tradings"] when e.Request.Method == HttpMethod.POST:
                    CreateTrade(e);
                    return true;
                case ["tradings", var tradeId] when e.Request.Method == HttpMethod.DELETE:
                    DeleteTrade(e, tradeId);
                    return true;
                case ["tradings", var tradeId] when e.Request.Method == HttpMethod.POST:
                    DoTrade(e, tradeId);
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

    public async void GetAllTrades(HttpRequest request)
    {
        var token = httpRequestEventArgs.GetBearerToken();

        if (token is null)
        {
            httpRequestEventArgs.Reply(HttpStatusCode.Unauthorized, "Access token is missing or invalid");
            return;
        }

        var authenticatedUser = await _userRepository.FindByUsernameAsync(TokenUtil.GetUsernameFromToken(token));

        if (authenticatedUser is null)
        {
            httpRequestEventArgs.Reply(HttpStatusCode.Unauthorized, "Access token is missing or invalid");
            return;
        }

        var allTrades = (await _tradeRepository.AllAsync()).ToList();

        if (!allTrades.Any())
        {
            httpRequestEventArgs.Reply(HttpStatusCode.NoContent, null);
            return;
        }

        httpRequestEventArgs.Reply(HttpStatusCode.OK, JsonConvert.SerializeObject(allTrades, Formatting.Indented));
    }

    public async void CreateTrade(HttpRequest request)
    {
        var token = httpRequestEventArgs.GetBearerToken();

        if (token is null)
        {
            httpRequestEventArgs.Reply(HttpStatusCode.Unauthorized, "Access token is missing or invalid");
            return;
        }

        var authenticatedUser = await _userRepository.FindByUsernameAsync(TokenUtil.GetUsernameFromToken(token));

        if (authenticatedUser is null)
        {
            httpRequestEventArgs.Reply(HttpStatusCode.Unauthorized, "Access token is missing or invalid");
            return;
        }

        CreateTrade? createTrade;

        var settings = new JsonSerializerSettings
        {
            Error = (_, args) =>
            {
                args.ErrorContext.Handled = true;
                createTrade = null;
            },
            MissingMemberHandling = MissingMemberHandling.Error,
        };

        createTrade = JsonConvert.DeserializeObject<CreateTrade>(httpRequestEventArgs.Request.Payload, settings);

        if (createTrade is null)
        {
            httpRequestEventArgs.Reply(HttpStatusCode.BadRequest, "Trade missing or invalid");
            return;
        }

        if (await _tradeRepository.ExistsByIdAsync(createTrade.Id))
        {
            httpRequestEventArgs.Reply(HttpStatusCode.Conflict, "A deal with this deal ID already exists");
            return;
        }

        var cardToTrade = await _cardRepository.FindCardByIdAsync(createTrade.CardToTrade);

        if (createTrade is null ||
            !await _userRepository.HasCardFromIdAsync(authenticatedUser, cardToTrade!.Id) ||
            TupleUtil.GetListFromTuple<Card>(authenticatedUser.Deck.Cards).Contains(cardToTrade))
        {
            httpRequestEventArgs.Reply(HttpStatusCode.Forbidden, "The deal contains a card that is not owned by the user or locked in the deck");
            return;
        }

        var trade = Trade.Create(
            createTrade.Id,
            cardToTrade,
            createTrade.Type,
            createTrade.MinimumDamage
        );

        await _tradeRepository.CreateTradeAsync(trade, authenticatedUser.Id);

        httpRequestEventArgs.Reply(HttpStatusCode.Created, "Trading deal successfully created");
    }

    public async void DeleteTrade(HttpRequest request, string tradeId)
    {
        var token = httpRequestEventArgs.GetBearerToken();

        if (token is null)
        {
            httpRequestEventArgs.Reply(HttpStatusCode.Unauthorized, "Access token is missing or invalid");
            return;
        }

        var authenticatedUser = await _userRepository.FindByUsernameAsync(TokenUtil.GetUsernameFromToken(token));

        if (authenticatedUser is null)
        {
            httpRequestEventArgs.Reply(HttpStatusCode.Unauthorized, "Access token is missing or invalid");
            return;
        }

        var trade = await _tradeRepository.FindTradeByIdAsync(tradeId);

        if (trade is null)
        {
            httpRequestEventArgs.Reply(HttpStatusCode.NotFound, "Trade with provided id was not found");
            return;
        }

        if (!authenticatedUser.ActiveTrades.Contains(trade))
        {
            httpRequestEventArgs.Reply(HttpStatusCode.Forbidden, "Trade contains card not owned by the user");
            return;
        }

        await _tradeRepository.DeleteTradeByIdAsync(trade.Id);
        
        httpRequestEventArgs.Reply(HttpStatusCode.OK, "Trade was deleted successfully");
    }

    public async void DoTrade(HttpRequest request, string tradeId)
    {
        var token = httpRequestEventArgs.GetBearerToken();

        if (token is null)
        {
            httpRequestEventArgs.Reply(HttpStatusCode.Unauthorized, "Access token is missing or invalid");
            return;
        }

        var authenticatedUser = await _userRepository.FindByUsernameAsync(TokenUtil.GetUsernameFromToken(token));

        if (authenticatedUser is null)
        {
            httpRequestEventArgs.Reply(HttpStatusCode.Unauthorized, "Access token is missing or invalid");
            return;
        }
        
        string? offeredCardId;

        var settings = new JsonSerializerSettings
        {
            Error = (_, args) =>
            {
                args.ErrorContext.Handled = true;
                offeredCardId = null;
            },
            MissingMemberHandling = MissingMemberHandling.Error,
        };

        offeredCardId = JsonConvert.DeserializeObject<string>(httpRequestEventArgs.Request.Payload, settings);

        if (offeredCardId is null)
        {
            httpRequestEventArgs.Reply(HttpStatusCode.BadRequest, "Card id missing or invalid");
            return;
        }

        var offeredCard = await _cardRepository.FindCardByIdAsync(offeredCardId);

        if (offeredCard is null)
        {
            httpRequestEventArgs.Reply(HttpStatusCode.BadRequest, "The offered card does not exist");
            return;
        }

        if (!await _userRepository.HasCardFromIdAsync(authenticatedUser, offeredCard.Id))
        {
            httpRequestEventArgs.Reply(HttpStatusCode.Forbidden, "The offered card is not owned by the user");
            return;
        }

        if (TupleUtil.GetListFromTuple<Card>(authenticatedUser.Deck.Cards).Contains(offeredCard))
        {
            httpRequestEventArgs.Reply(HttpStatusCode.Forbidden, "The offered card is locked in the user's deck");
            return;
        }

        var trade = await _tradeRepository.FindTradeByIdAsync(tradeId);

        if (trade is null)
        {
            httpRequestEventArgs.Reply(HttpStatusCode.NotFound, "Trade with provided id was not found");
            return;
        }

        if (authenticatedUser.Id == await _tradeRepository.FindCreatorUserIdByTradeId(trade.Id))
        {
            httpRequestEventArgs.Reply(HttpStatusCode.Forbidden, "User cannot trade with himself");
            return;
        }

        if (offeredCard.CardType != trade.Type ||
            offeredCard.Damage < trade.MinimumDamage)
        {
            httpRequestEventArgs.Reply(HttpStatusCode.Forbidden, "The offered card does not meet the trade requirements");
            return;
        }

        var tradeCreatorId = (await _tradeRepository.FindCreatorUserIdByTradeId(trade.Id))!;
        var tradeCreator = (await _userRepository.FindByIdAsync(tradeCreatorId))!;

        tradeCreator.Collection.Add(offeredCard);
        tradeCreator.Collection.Remove(trade.CardToTrade);
        authenticatedUser.Collection.Add(trade.CardToTrade);
        authenticatedUser.Collection.Remove(offeredCard);

        await _userRepository.UpdateUserAsync(tradeCreator);
        await _userRepository.UpdateUserAsync(authenticatedUser);
        await _tradeRepository.DeleteTradeByIdAsync(trade.Id);
        
        httpRequestEventArgs.Reply(HttpStatusCode.OK, "Trading deal successfully executed");
    }
}