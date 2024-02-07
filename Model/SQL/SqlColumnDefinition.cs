namespace MonsterTCG.Model.SQL
{
    public static class SqlDataType
    {
        public static string Serial => "SERIAL";
        public static string Integer => "INTEGER";
        public static string Varchar20 => "VARCHAR(20)";
        public static string Varchar50 => "VARCHAR(50)";
        public static string Varchar200 => "VARCHAR(200)";
    }

    public class SqlForeignKey
    {
        public string TableName { get; }
        public string ColumnName { get; }

        public SqlForeignKey(string tableName, string columnName)
        {
            TableName = tableName;
            ColumnName = columnName;
        }
    }

    public class SqlColumnDefinition
    {
        public string Name { get; set; }
        public string DataType { get; set; }
        public bool IsNullable { get; set; }
        public bool IsPrimaryKey { get; set; }
        public bool IsUnique { get; set; }
        public SqlForeignKey? ForeignKey { get; set; }
        public string? AdditionalArguments { get; set; }

        public SqlColumnDefinition(string name, string dataType, bool isNullable = true, bool isPrimaryKey = false, bool isUnique = false, SqlForeignKey? foreignKey = null, string? additionalArguments = null)
        {
            Name = name;
            DataType = dataType;
            IsNullable = isNullable;
            IsPrimaryKey = isPrimaryKey;
            IsUnique = isUnique;
            ForeignKey = foreignKey;
            AdditionalArguments = additionalArguments;
        }
    }
}
