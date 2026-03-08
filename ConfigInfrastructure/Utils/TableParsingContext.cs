using System.Collections.Generic;

namespace ConfigGenerator.ConfigInfrastructure.Utils;

public sealed class TableParsingContext
{
    public string TableName { get; }
    public int PrimaryKey { get; }
    public int StartRow { get; }
    public int StartCol { get; }
    public IList<IList<object>> PageData { get; }

    public TableParsingContext(string tableName, int primaryKey, int startRow, int startCol, IList<IList<object>> pageData)
    {
        TableName = tableName;
        PrimaryKey = primaryKey;
        StartRow = startRow;
        StartCol = startCol;
        PageData = pageData;
    }
}
