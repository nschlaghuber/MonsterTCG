namespace MonsterTCG.Model.SQL;

public class SqlTableDefinition
{
    public string Name { get; }
    public List<SqlColumnDefinition> Columns { get; }
    public List<string>? AdditionalLines { get; }

    public SqlTableDefinition(string name, List<SqlColumnDefinition> columns, List<string>? additionalLines = null)
    {
        Name = name;
        Columns = columns;
        AdditionalLines = additionalLines;
    }
}