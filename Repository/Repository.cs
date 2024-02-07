using MonsterTCG.Model.SQL;
using Npgsql;

namespace MonsterTCG.Repository
{
    public abstract class Repository
    {
        protected NpgsqlDataSource DataSource { get; }
        
        protected Repository(NpgsqlDataSource dataSource)
        {
            DataSource = dataSource;
        }
    }
}