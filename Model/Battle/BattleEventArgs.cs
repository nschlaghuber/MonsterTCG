namespace MonsterTCG.Model.Battle;

public class BattleEventArgs : EventArgs
{
    public Outcome Outcome { get; }
    public int AcquiredCoins { get; }
    public int AcquiredEloScore { get; }
    public string BattleLog { get; }
    
    public BattleEventArgs(Outcome outcome, int acquiredEloScore, int acquiredCoins, string battleLog)
    {
        Outcome = outcome;
        AcquiredEloScore = acquiredEloScore;
        AcquiredCoins = acquiredCoins;
        BattleLog = battleLog;
    }
}