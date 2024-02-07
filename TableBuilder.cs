using MonsterTCG.Model.SQL;
using Npgsql;

namespace MonsterTCG;

public class TableBuilder
{
    NpgsqlDataSource DataSource { get; }

    readonly List<SqlTableDefinition> _tables = new List<SqlTableDefinition>()
    {
        new SqlTableDefinition(
            "user_data",
            new List<SqlColumnDefinition>()
            {
                new("user_data_id", SqlDataType.Varchar50, isPrimaryKey: true),
                new("name", SqlDataType.Varchar50),
                new("bio", SqlDataType.Varchar200),
                new("image", SqlDataType.Varchar50)
            }),
        new SqlTableDefinition(
            "user_stats",
            new List<SqlColumnDefinition>()
            {
                new("user_stats_id", SqlDataType.Varchar50, isPrimaryKey: true),
                new("elo_score", SqlDataType.Integer, isNullable: false),
                new("wins", SqlDataType.Integer, isNullable: false),
                new("losses", SqlDataType.Integer, isNullable: false),
            }
        ),
        new SqlTableDefinition(
            "user",
            new List<SqlColumnDefinition>()
            {
                new("user_id", SqlDataType.Varchar50, isPrimaryKey: true),
                new("username", SqlDataType.Varchar20, isNullable: false, isUnique: true),
                new("password", SqlDataType.Varchar50, isNullable: false),
                new("user_data_id", SqlDataType.Varchar50,
                    foreignKey: new SqlForeignKey("user_data", "user_data_id")),
                new("coins", SqlDataType.Integer, isNullable: false),
                new("user_stats_id", SqlDataType.Varchar50,
                    foreignKey: new SqlForeignKey("user_stats", "user_stats_id"))
            }
        ),
        new SqlTableDefinition(
            "card",
            new List<SqlColumnDefinition>()
            {
                new("card_id", SqlDataType.Varchar50, isPrimaryKey: true),
                new("name", SqlDataType.Varchar20, isNullable: false),
                new("damage", SqlDataType.Integer, isNullable: false),
                new("element_type", SqlDataType.Varchar20, isNullable: false),
                new("card_type", SqlDataType.Varchar20, isNullable: false),
            }
        ),
        new SqlTableDefinition(
            "package",
            new List<SqlColumnDefinition>()
            {
                new("package_id", SqlDataType.Varchar50, isPrimaryKey: true),
                new("card_id_1", SqlDataType.Varchar50, foreignKey: new SqlForeignKey("card", "card_id")),
                new("card_id_2", SqlDataType.Varchar50, foreignKey: new SqlForeignKey("card", "card_id")),
                new("card_id_3", SqlDataType.Varchar50, foreignKey: new SqlForeignKey("card", "card_id")),
                new("card_id_4", SqlDataType.Varchar50, foreignKey: new SqlForeignKey("card", "card_id")),
                new("card_id_5", SqlDataType.Varchar50, foreignKey: new SqlForeignKey("card", "card_id")),
            }
        ),
        new SqlTableDefinition(
            "user_card",
            new List<SqlColumnDefinition>()
            {
                new("user_id", SqlDataType.Varchar50, foreignKey: new SqlForeignKey("user", "user_id")),
                new("card_id", SqlDataType.Varchar50, foreignKey: new SqlForeignKey("card", "card_id"))
            },
            new List<string>()
            {
                "PRIMARY KEY (user_id, card_id)"
            }
        ),
        new SqlTableDefinition(
            "deck",
            new List<SqlColumnDefinition>()
            {
                new("deck_id", SqlDataType.Varchar50, isPrimaryKey: true)
            }
        )
    }; 

    public TableBuilder(NpgsqlDataSource dataSource)
    {
        DataSource = dataSource;
    }

    public async Task DropTables()
    {
        foreach (var table in _tables.AsEnumerable().Reverse().ToList())
        {
            await DropTable(table);
        }
    }

    private async Task DropTable(SqlTableDefinition table)
    {
        await using var dropTableCommand = DataSource.CreateCommand($"DROP TABLE public.{table.Name}");

        await dropTableCommand.ExecuteNonQueryAsync();
    }

    public async Task EnsureTablesExists()
    {
        foreach (var table in _tables)
        {
            await CreateTableIfNotExists(table);
        }
    }

    private async Task CreateTableIfNotExists(SqlTableDefinition table)
    {
        await using var createTableCommand = DataSource.CreateCommand(
            $"CREATE TABLE IF NOT EXISTS public.\"{table.Name}\" (" +
            string.Join(",", table.Columns.Select(col => "\n" +
                                                         $"\"{col.Name}\" " +
                                                         col.DataType +
                                                         (!col.IsNullable ? " NOT NULL" : "") +
                                                         (col.IsUnique ? " UNIQUE" : "") +
                                                         (col.IsPrimaryKey ? " PRIMARY KEY" : "") +
                                                         (col.ForeignKey != null
                                                             ? $" REFERENCES public.\"{col.ForeignKey.TableName}\"(\"{col.ForeignKey.ColumnName}\")"
                                                             : "")
            )) +
            (table.AdditionalLines != null ? "\n," + string.Join(",\n", table.AdditionalLines) : "") +
            ")");

        await createTableCommand.ExecuteNonQueryAsync();
    }
}