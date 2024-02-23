using Npgsql;

namespace MonsterTCG.Repository;

public class BattleRepository : IBattleRepository
{
    private readonly NpgsqlDataSource _dataSource;

    public BattleRepository(NpgsqlDataSource dataSource)
    {
        _dataSource = dataSource;
    }
}