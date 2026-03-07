using System.Collections.Generic;
using ConfigGenerator.ConfigInfrastructure.Data;

namespace ConfigGenerator.ConfigInfrastructure.Utils
{
    public static class TableExtractionService
    {
        public static bool TryExtractTables(string pageName, IList<IList<object>> pageData, out List<TableData> tableDataList)
        {
            return TableExtractionEngine.ExtractTablesFromPage(pageName, pageData, out tableDataList);
        }
    }
}
