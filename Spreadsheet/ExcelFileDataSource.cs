using System.Collections.Generic;
using System.Threading.Tasks;
using ClosedXML.Excel;

namespace ConfigGenerator.Spreadsheet;

public class ExcelFileDataSource : ISpreadsheetDataSource
{
    private readonly string _filePath;

    public ExcelFileDataSource(string filePath)
    {
        _filePath = filePath;
    }

    public async Task<List<SpreadsheetPageData>> GetAllSheetsDataAsync()
    {
        return await Task.Run(() =>
        {
            var pages = new List<SpreadsheetPageData>();

            using (var workbook = new XLWorkbook(_filePath))
            {
                foreach (var worksheet in workbook.Worksheets)
                {
                    var page = new SpreadsheetPageData
                    {
                        name = worksheet.Name,
                        values = new List<IList<object>>()
                    };

                    var range = worksheet.RangeUsed();
                    if (range != null)
                    {
                        foreach (var row in range.Rows())
                        {
                            var rowData = new List<object>();
                            foreach (var cell in row.Cells())
                            {
                                var cellText = cell.GetString();
                                rowData.Add(cellText);
                            }
                            page.values.Add(rowData);
                        }
                    }

                    pages.Add(page);
                }
            }

            return pages;
        });
    }
}