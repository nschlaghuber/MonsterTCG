namespace MonsterTCG.Model.Battle;

public class BattleRequest
{
    public User.User BattlingUser { get; }
    public Bet Bet { get; }

    public delegate void BattleEventHandler(object source, BattleEventArgs args);

    public event BattleEventHandler? BattleFinished;
    
    
    public BattleRequest(User.User battlingUser, Bet bet)
    {
        BattlingUser = battlingUser;
        Bet = bet;
    }

    public void OnBattleFinished(BattleEventArgs args)
    {
        BattleFinished?.Invoke(this, args);
    }
}