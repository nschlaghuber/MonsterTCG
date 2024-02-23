using System.Collections.Concurrent;
using System.Text;
using System.Threading.Channels;
using MonsterTCG.Model.Battle;
using MonsterTCG.Model.Card;
using MonsterTCG.Util;

namespace MonsterTCG.Service.BattleService;

public class BattleService : IBattleService
{
    private readonly List<BattleRequest> _waitingRequests = new();
    private readonly object _listLock = new();

    public void QueueForBattle(BattleRequest battleRequest)
    {
        lock (_listLock)
        {
            _waitingRequests.Add(battleRequest);
            Console.WriteLine($"Battle request recieved by user \"{battleRequest.BattlingUser.Username}\"");
        }

        Thread.Sleep(1000);

        BattleRequest? player1Request = null;
        BattleRequest? player2Request = null;

        lock (_listLock)
        {
            if (_waitingRequests.Count >= 2)
            {
                player1Request = _waitingRequests.First();
                _waitingRequests.Remove(player1Request);

                player2Request = _waitingRequests.First();

                _waitingRequests.Where(request => request.Bet == player1Request.Bet).ToList().ForEach(request =>
                {
                    if (Math.Abs(player1Request.BattlingUser.UserStats.EloScore -
                                 request.BattlingUser.UserStats.EloScore) <
                        Math.Abs(player1Request.BattlingUser.UserStats.EloScore -
                                 player2Request.BattlingUser.UserStats.EloScore))
                    {
                        player2Request = request;
                    }
                });

                _waitingRequests.Remove(player2Request);
            }
        }

        if (player1Request is null || player2Request is null)
        {
            return;
        }

        DoBattle(player1Request, player2Request);
    }

    private void DoBattle(BattleRequest player1Request, BattleRequest player2Request)
    {
        Console.WriteLine(
            $"Battle started between \"{player1Request.BattlingUser.Username}\" and \"{player2Request.BattlingUser.Username}\"");

        var log = new StringBuilder();

        // Ease of access, player 1 is not "player 0" and so on
        var player = new List<Player>
        {
            new(player1Request.BattlingUser, player1Request.Bet),
            new(player2Request.BattlingUser, player2Request.Bet),
        };

        log.AppendLine($"{player[0].Name} vs {player[1].Name}");
        log.AppendLine();
        log.AppendLine("May the battle commence!");

        var winner = -1;
        var loser = -1;

        int rounds;

        for (rounds = 1; rounds < 100 && player[0].Deck.Count > 0 && player[1].Deck.Count > 0; rounds++)
        {
            var drawnCard = player.Select(p => p.Deck[Random.Shared.Next(0, p.Deck.Count)]).ToList();

            var calculatedCardDamage = new List<int>
            {
                CalculateDamage(drawnCard[0], drawnCard[1]),
                CalculateDamage(drawnCard[1], drawnCard[0]),
            };
            
            log.AppendLine();
            log.Append($"{player[0].Name}: {drawnCard[0].Name} ({drawnCard[0].Damage} Damage) vs {player[1].Name}: {drawnCard[1].Name} ({drawnCard[1].Damage} Damage) => ");
            if (calculatedCardDamage.Min() != calculatedCardDamage.Max())
            {
                var winningCard = calculatedCardDamage.IndexOf(calculatedCardDamage.Max());
                var losingCard = calculatedCardDamage.IndexOf(calculatedCardDamage.Min());

                player[losingCard].Deck.Remove(drawnCard[losingCard]);
                player[winningCard].Deck.Add(drawnCard[losingCard]);

                log.AppendLine($"{drawnCard[winningCard].Name} ({calculatedCardDamage[winningCard]}) defeats {drawnCard[losingCard].Name} ({calculatedCardDamage[losingCard]})");
                log.AppendLine($"{player[winningCard].Name} steals {player[losingCard].Name}'s {drawnCard[losingCard].Name} and now has {player[winningCard].Deck.Count} cards");
            }
            else
            {
                log.AppendLine($"{calculatedCardDamage[0]} vs {calculatedCardDamage[1]} => Draw, decks remain unchanged");
            }
        }

        var draw = false;

        if (rounds == 100)
        {
            draw = true;
        }
        else
        {
            winner = player.IndexOf(player.First(p => p.Deck.Count > 0));
            loser = player.IndexOf(player.First(p => p.Deck.Count == 0));
        }

        var acquiredBets = player
            .Select(p => draw ? 0 : p == player[winner] ? (int)player[loser].Bet : -(int)player[loser].Bet).ToList();

        var acquiredEloScore = player.Select(p =>
            draw ? 0 : 
            p == player[winner] ? 
                3 : -5).ToList();


        log.AppendLine();
        log.AppendLine("FINISHED!");
        log.AppendLine();
        log.AppendLine("====================================");
        log.AppendLine();
        if (!draw)
        {
            log.AppendLine($"{player[winner].Name} has defeated {player[loser].Name}");
            log.AppendLine();
            log.AppendLine(
                $"{player[winner].Name} will receive {(int)player[loser].Bet} coins from {player[loser].Name}");
            log.AppendLine($"{player[winner].Name} will return all of {player[loser].Name}'s cards");
            log.AppendLine(
                $"{player[0].Name} ELO: {player[0].EloScore} -> {player[0].EloScore + acquiredEloScore[0]}{(acquiredEloScore[0] > 0 ? "^" : "v")}");
            log.AppendLine(
                $"{player[1].Name} ELO: {player[1].EloScore} -> {player[1].EloScore + acquiredEloScore[1]}{(acquiredEloScore[1] > 0 ? "^" : "v")}");
        }
        else
        {
            log.AppendLine("Draw!");
            log.AppendLine();
            log.AppendLine("Players will not lose or gain any coins");
            log.AppendLine("Players will return all stolen cards");
            log.AppendLine("Players will not lose or receive any ELO score");
        }

        log.AppendLine();
        log.AppendLine("Thanks for playing!");

        var outputLog = log.ToString();

        // Assume both bets are equal
        player1Request.OnBattleFinished(new BattleEventArgs(
            draw ? Outcome.Draw : winner == 0 ? Outcome.Victory : Outcome.Defeat,
            acquiredEloScore[0],
            acquiredBets[0],
            outputLog));
        player2Request.OnBattleFinished(new BattleEventArgs(
            draw ? Outcome.Draw : winner == 1 ? Outcome.Victory : Outcome.Defeat,
            acquiredEloScore[1],
            acquiredBets[1],
            outputLog));
    }

    private int CalculateDamage(Card attacker, Card defender)
    {
        if (attacker.Name.Contains("Goblin") && defender.Name.Contains("Dragon") ||
            attacker.Name.Contains("Ork") && defender.Name.Contains("Wizzard") ||
            attacker.Name.Contains("Knight") && defender.Name.Contains("WaterSpell") ||
            attacker.CardType == CardType.Spell && defender.Name.Contains("Kraken") ||
            attacker.Name.Contains("Dragon") && defender.Name.Contains("FireElf"))
        {
            return 0;
        }

        if (attacker.CardType == CardType.Monster && defender.CardType == CardType.Monster)
        {
            return attacker.Damage;
        }

        return (attacker.ElementType, defender.ElementType) switch
        {
            (ElementType.Water, ElementType.Fire) => attacker.Damage * 2,
            (ElementType.Fire, ElementType.Normal) => attacker.Damage * 2,
            (ElementType.Normal, ElementType.Water) => attacker.Damage * 2,
            (ElementType.Fire, ElementType.Fire) => attacker.Damage,
            (ElementType.Water, ElementType.Water) => attacker.Damage,
            (ElementType.Normal, ElementType.Normal) => attacker.Damage,
            (ElementType.Fire, ElementType.Water) => attacker.Damage / 2,
            (ElementType.Normal, ElementType.Fire) => attacker.Damage / 2,
            (ElementType.Water, ElementType.Normal) => attacker.Damage / 2,
            _ => -1
        };
    }
}