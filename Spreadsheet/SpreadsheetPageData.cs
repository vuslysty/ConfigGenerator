using System.Collections.Generic;

namespace ConfigGenerator.Spreadsheet;

public class SpreadsheetPageData
{
    public string name;
    public IList<IList<object>> values;
}