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
            switch (pathParts)
            {
                case ["cards"] when e.Request.Method == HttpMethod.GET:
                    GetCards(e);
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
                            e.Reply(HttpStatusCode.BadRequest, "Invalid Parameters");
                        }
                    }
                    GetDeck(e, formatType);
                    return true;
                case ["deck"] when e.Request.Method == HttpMethod.PUT:
                    ConfigureDeck(e);
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

    public async void GetCards(HttpRequest request)
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

        if (!authenticatedUser.Collection.Any())
        {
            httpRequestEventArgs.Reply(HttpStatusCode.NoContent, "User does not have any cards");
            return;
        }
        
        httpRequestEventArgs.Reply(HttpStatusCode.OK, JsonConvert.SerializeObject(authenticatedUser.Collection, Formatting.Indented));
    }

    public async void GetDeck(HttpRequest request, FormatType formatType)
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
        
        if (!authenticatedUser.DeckCardsAsList.OfType<Card>().Any())
        {
            httpRequestEventArgs.Reply(HttpStatusCode.NoContent, "Deck is empty");
            return;
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
        
        httpRequestEventArgs.Reply(HttpStatusCode.OK, responseMessage, formatType);
    }

    public async void ConfigureDeck(HttpRequest request)
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

        var cardIds = JsonConvert.DeserializeObject<List<string>>(httpRequestEventArgs.Request.Payload);

        if (cardIds is not { Count: 4 })
        {
            httpRequestEventArgs.Reply(HttpStatusCode.BadRequest, "The provided deck did not include the required amount of cards");
            return;
        }

        if (!await _userRepository.HasCardsFromIdsAsync(authenticatedUser, cardIds))
        {
            httpRequestEventArgs.Reply(HttpStatusCode.Forbidden, "At least one of the provided cards does not belong to the user or is not available.");
            return;
        }
        
        var newDeckList = (await _cardRepository.FindCardsByIdsAsync(cardIds))?.ToList();

        authenticatedUser.Deck.Cards = (newDeckList?[0], newDeckList?[1], newDeckList?[2], newDeckList?[3])!;

        await _userRepository.UpdateUserAsync(authenticatedUser);
        
        httpRequestEventArgs.Reply(HttpStatusCode.OK, "The deck has been successfully configured");
    }
}