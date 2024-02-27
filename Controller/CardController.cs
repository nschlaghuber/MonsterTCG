using System.Net;
using System.Text;
using MonsterTCG.Model;
using MonsterTCG.Model.Card;
using MonsterTCG.Model.Deck;
using MonsterTCG.Model.Http;
using MonsterTCG.Repository;
using MonsterTCG.Util;
using Newtonsoft.Json;
using HttpMethod = MonsterTCG.Model.Http.HttpMethod;

namespace MonsterTCG.Controller;

public class CardController : Controller
{
    private readonly IUserRepository _userRepository;
    private readonly ICardRepository _cardRepository;
    
    public CardController(ICardRepository cardRepository, IUserRepository userRepository)
    {
        _userRepository = userRepository;
        _cardRepository = cardRepository;
    }
    public override async Task<bool> ProcessRequest(HttpRequestEventArgs e)
    {
        try
        {
            var parts = e.Request.Path.Split('?');
            var pathParts = parts[0].TrimStart('/').Split('/');
            var parameters = parts.Length > 1 ? parts[1] : "";

            HttpResponse? response;
            switch (pathParts)
            {
                case ["cards"] when e.Request.Method == HttpMethod.GET:
                    e.Reply(await GetCards(e.Request));
                    return true;
                case ["deck"] when e.Request.Method == HttpMethod.GET:
                    var formatType = FormatType.Json;
                    var paramParts = !string.IsNullOrEmpty(parameters) ? parameters.Split('=') : Array.Empty<string>();
                    if (paramParts is ["format", var format, ..])
                    {
                        try
                        {
                            formatType = Enum.Parse<FormatType>(format, true);
                        }
                        catch (Exception)
                        {
                            e.Reply(new HttpResponse(HttpStatusCode.BadRequest, "Invalid Parameters"));
                        }
                    }
                    e.Reply(await GetDeck(e.Request, formatType));
                    return true;
                case ["deck"] when e.Request.Method == HttpMethod.PUT:
                    e.Reply(await ConfigureDeck(e.Request));
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

    public async Task<HttpResponse> GetCards(HttpRequest request)
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

        if (!authenticatedUser.Collection.Any())
        {
            return new HttpResponse(HttpStatusCode.NoContent, "User does not have any cards");
        }
        
        return new HttpResponse(HttpStatusCode.OK, JsonConvert.SerializeObject(authenticatedUser.Collection, Formatting.Indented));
    }

    public async Task<HttpResponse> GetDeck(HttpRequest request, FormatType formatType)
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
        
        if (!authenticatedUser.DeckCardsAsList.OfType<Card>().Any())
        {
            return new HttpResponse(HttpStatusCode.NoContent, "Deck is empty");
        }

        var responseMessage = "";

        switch (formatType)
        {
            case FormatType.Json:
                responseMessage = JsonConvert.SerializeObject(authenticatedUser.DeckCardsAsList, Formatting.Indented);
                break;
            case FormatType.Plain:
                responseMessage = string.Join(", ", authenticatedUser.DeckCardsAsList.Select(card => card!.Name));
                break;
        }
        
        return new HttpResponse(HttpStatusCode.OK, responseMessage, formatType);
    }

    public async Task<HttpResponse> ConfigureDeck(HttpRequest request)
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

        var cardIds = JsonConvert.DeserializeObject<List<string>>(request.Payload);

        if (cardIds is not { Count: 4 })
        {
            return new HttpResponse(HttpStatusCode.BadRequest, "The provided deck did not include the required amount of cards");
        }

        if (!await _userRepository.HasCardsFromIdsAsync(authenticatedUser, cardIds))
        {
            return new HttpResponse(HttpStatusCode.Forbidden, "At least one of the provided cards does not belong to the user or is not available.");
        }
        
        var newDeckList = (await _cardRepository.FindCardsByIdsAsync(cardIds))?.ToList();

        authenticatedUser.Deck.Cards = (newDeckList?[0], newDeckList?[1], newDeckList?[2], newDeckList?[3])!;

        await _userRepository.UpdateUserAsync(authenticatedUser);
        
        return new HttpResponse(HttpStatusCode.OK, "The deck has been successfully configured");
    }
}