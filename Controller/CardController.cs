using System.Net;
using MonsterTCG.Model;
using MonsterTCG.Model.Http;
using MonsterTCG.Repository;
using MonsterTCG.Util;
using Newtonsoft.Json;
using HttpMethod = MonsterTCG.Model.Http.HttpMethod;

namespace MonsterTCG.Controller;

public class CardController : Controller
{
    private UserRepository _userRepository;
    private CardRepository _cardRepository;
    
    public CardController(CardRepository cardRepository, UserRepository userRepository)
    {
        _userRepository = userRepository;
        _cardRepository = cardRepository;
    }
    public override bool ProcessRequest(HttpRequestEventArgs e)
    {
        try
        {
            var test = e.Request.Path.TrimStart('/').Split('/');
            switch (test)
            {
                case ["cards"] when e.Request.Method == HttpMethod.GET:
                    GetCards(e);
                    return true;
                case ["deck"] when e.Request.Method == HttpMethod.GET:
                    GetDeck(e);
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

    public async void GetCards(HttpRequestEventArgs httpRequestEventArgs)
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

        if (!authenticatedUser.Collection.Any())
        {
            httpRequestEventArgs.Reply(HttpStatusCode.NoContent, "User does not have any cards");
            return;
        }
        
        httpRequestEventArgs.Reply(HttpStatusCode.OK, JsonConvert.SerializeObject(authenticatedUser.Collection, Formatting.Indented));
    }

    public async void GetDeck(HttpRequestEventArgs httpRequestEventArgs)
    {
        var token = httpRequestEventArgs.GetBearerToken();

        if (token is null)
        {
            httpRequestEventArgs.Reply(HttpStatusCode.Unauthorized, "Access token is missing or invalid");
            return;
        }

        var authenticatedUser = await _userRepository.FindUserByUsername(TokenUtil.GetUsernameFromToken(token));

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
        
        httpRequestEventArgs.Reply(HttpStatusCode.OK, JsonConvert.SerializeObject(authenticatedUser.DeckCardsAsList, Formatting.Indented));
    }

    public async void ConfigureDeck(HttpRequestEventArgs httpRequestEventArgs)
    {
        var token = httpRequestEventArgs.GetBearerToken();

        if (token is null)
        {
            httpRequestEventArgs.Reply(HttpStatusCode.Unauthorized, "Access token is missing or invalid");
            return;
        }

        var authenticatedUser = await _userRepository.FindUserByUsername(TokenUtil.GetUsernameFromToken(token));

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

        if (!await _userRepository.HasCardsFromIds(authenticatedUser, cardIds))
        {
            httpRequestEventArgs.Reply(HttpStatusCode.Forbidden, "At least one of the provided cards does not belong to the user or is not available.");
            return;
        }
        
        var newDeckList = (await _cardRepository.FindCardsByIds(cardIds))?.ToList();

        authenticatedUser.Deck = (newDeckList?[0], newDeckList?[1], newDeckList?[2], newDeckList?[3])!;

        if (await _userRepository.UpdateUser(authenticatedUser) is null)
        {
            httpRequestEventArgs.Reply(HttpStatusCode.InternalServerError, "Something went wrong");
            return;
        }
        
        httpRequestEventArgs.Reply(HttpStatusCode.OK, "\t\nThe deck has been successfully configured");
    }
}