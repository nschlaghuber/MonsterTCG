using MonsterTCG.Model.Battle;

namespace MonsterTCG.Service.BattleService;

public interface IBattleService
{
    /// <summary>
    /// Queues a <see cref="BattleRequest"/> to enter battle.
    /// Upon completion of a battle, the <see cref="BattleRequest.BattleFinished"/> event on the <see cref="BattleRequest"/> will be called.
    /// </summary>
    /// <param name="battleRequest">The <see cref="BattleRequest"/> to be queued</param>
    public void QueueForBattle(BattleRequest battleRequest);
}