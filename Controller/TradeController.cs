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

            HttpResponse? response;
            switch (pathParts)
            {
                case ["tradings"] when e.Request.Method == HttpMethod.GET:
                    e.Reply(await GetAllTrades(e.Request));
                    return true;
                case ["tradings"] when e.Request.Method == HttpMethod.POST:
                    e.Reply(await CreateTrade(e.Request));
                    return true;
                case ["tradings", var tradeId] when e.Request.Method == HttpMethod.DELETE:
                    e.Reply(await DeleteTrade(e.Request, tradeId));
                    return true;
                case ["tradings", var tradeId] when e.Request.Method == HttpMethod.POST:
                    e.Reply(await DoTrade(e.Request, tradeId));
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

    public async Task<HttpResponse> GetAllTrades(HttpRequest request)
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

        var allTrades = (await _tradeRepository.AllAsync()).ToList();

        if (!allTrades.Any())
        {
            return new HttpResponse(HttpStatusCode.NoContent, null);
        }

        return new HttpResponse(HttpStatusCode.OK, JsonConvert.SerializeObject(allTrades, Formatting.Indented));
    }

    public async Task<HttpResponse> CreateTrade(HttpRequest request)
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

        createTrade = JsonConvert.DeserializeObject<CreateTrade>(request.Payload, settings);

        if (createTrade is null)
        {
            return new HttpResponse(HttpStatusCode.BadRequest, "Trade missing or invalid");
        }

        if (await _tradeRepository.ExistsByIdAsync(createTrade.Id))
        {
            return new HttpResponse(HttpStatusCode.Conflict, "A deal with this deal ID already exists");
        }

        var cardToTrade = await _cardRepository.FindCardByIdAsync(createTrade.CardToTrade);

        if (createTrade is null ||
            !await _userRepository.HasCardFromIdAsync(authenticatedUser, cardToTrade!.Id) ||
            TupleUtil.GetListFromTuple<Card>(authenticatedUser.Deck.Cards).Contains(cardToTrade))
        {
            return new HttpResponse(HttpStatusCode.Forbidden, "The deal contains a card that is not owned by the user or locked in the deck");
        }

        var trade = Trade.Create(
            createTrade.Id,
            cardToTrade,
            createTrade.Type,
            createTrade.MinimumDamage
        );

        await _tradeRepository.CreateTradeAsync(trade, authenticatedUser.Id);

        return new HttpResponse(HttpStatusCode.Created, "Trading deal successfully created");
    }

    public async Task<HttpResponse> DeleteTrade(HttpRequest request, string tradeId)
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

        var trade = await _tradeRepository.FindTradeByIdAsync(tradeId);

        if (trade is null)
        {
            return new HttpResponse(HttpStatusCode.NotFound, "Trade with provided id was not found");
        }

        if (!authenticatedUser.ActiveTrades.Contains(trade))
        {
            return new HttpResponse(HttpStatusCode.Forbidden, "Trade contains card not owned by the user");
        }

        await _tradeRepository.DeleteTradeByIdAsync(trade.Id);
        
        return new HttpResponse(HttpStatusCode.OK, "Trade was deleted successfully");
    }

    public async Task<HttpResponse> DoTrade(HttpRequest request, string tradeId)
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

        offeredCardId = JsonConvert.DeserializeObject<string>(request.Payload, settings);

        if (offeredCardId is null)
        {
            return new HttpResponse(HttpStatusCode.BadRequest, "Card id missing or invalid");
        }

        var offeredCard = await _cardRepository.FindCardByIdAsync(offeredCardId);

        if (offeredCard is null)
        {
            return new HttpResponse(HttpStatusCode.BadRequest, "The offered card does not exist");
        }

        if (!await _userRepository.HasCardFromIdAsync(authenticatedUser, offeredCard.Id))
        {
            return new HttpResponse(HttpStatusCode.Forbidden, "The offered card is not owned by the user");
        }

        if (TupleUtil.GetListFromTuple<Card>(authenticatedUser.Deck.Cards).Contains(offeredCard))
        {
            return new HttpResponse(HttpStatusCode.Forbidden, "The offered card is locked in the user's deck");
        }

        var trade = await _tradeRepository.FindTradeByIdAsync(tradeId);

        if (trade is null)
        {
            return new HttpResponse(HttpStatusCode.NotFound, "Trade with provided id was not found");
        }

        if (authenticatedUser.Id == await _tradeRepository.FindCreatorUserIdByTradeId(trade.Id))
        {
            return new HttpResponse(HttpStatusCode.Forbidden, "User cannot trade with himself");
        }

        if (offeredCard.CardType != trade.Type ||
            offeredCard.Damage < trade.MinimumDamage)
        {
            return new HttpResponse(HttpStatusCode.Forbidden, "The offered card does not meet the trade requirements");
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
        
        return new HttpResponse(HttpStatusCode.OK, "Trading deal successfully executed");
    }
}