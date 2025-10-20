using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using ConfigGenerator.ConfigInfrastructure;
using ConfigGenerator.ConfigInfrastructure.Data;
using ConfigGenerator.ConfigInfrastructure.TypeDesctiptors;
using ConfigGenerator.ConfigInfrastructure.Utils;
using ConfigGenerator.Spreadsheet;

namespace ConfigGenerator;

public class ConfigGenerator
{
    private readonly ITableDataSerializer _tableDataSerializer;
    private readonly string _className;
    private readonly string _namespaceName;

    public ConfigGenerator(ITableDataSerializer tableDataSerializer, string className, string namespaceName)
    {
        _className = className;
        _namespaceName = namespaceName;
        _tableDataSerializer = tableDataSerializer;
    }

    public void generateCode(string json, string outputFolderPath)
    {
        List<TableData> allTables = _tableDataSerializer.Deserialize(json);
        generateCode(allTables, outputFolderPath);
    }
    
    public async Task generateCode(List<ISpreadsheetDataSource> spreadsheetSources, string outputFolderPath)
    {
        List<TableData> allTables = new List<TableData>();
        bool parseResult = await tryParseTables(spreadsheetSources, allTables);

        if (!parseResult) {
            return;
        }
        
        generateCode(allTables, outputFolderPath);
    }
    
    public void generateCode(List<TableData> tables, string outputFolderPath)
    {
        string code = CodeGenerator.GenerateConfigClasses(tables, _className, _namespaceName);
        
        Directory.CreateDirectory(outputFolderPath);
        
        // 📝 Формуємо шлях до файлу
        string filePath = Path.Combine(outputFolderPath, $"{_className}.cs");
        File.WriteAllTextAsync(filePath, code);
        
        Console.WriteLine($"✅ Файл збережено: {filePath}");
    }

    public async Task generateJson(List<ISpreadsheetDataSource> spreadsheetSources, string outputFolderPath)
    {
        List<TableData> allTables = new List<TableData>();
        bool parseResult = await tryParseTables(spreadsheetSources, allTables);

        if (!parseResult) {
            return;
        }
        
        generateJson(allTables, outputFolderPath);
    }
    
    public void generateJson(List<TableData> tables, string outputFolderPath)
    {
        string json = _tableDataSerializer.Serialize(tables);
        
        Directory.CreateDirectory(outputFolderPath);
        
        // 📝 Формуємо шлях до файлу
        string filePath = Path.Combine(outputFolderPath, $"{_className}.json");
        File.WriteAllTextAsync(filePath, json);
        
        Console.WriteLine($"✅ Файл збережено: {filePath}");
    }

    public void generate(List<ISpreadsheetDataSource> spreadsheetSources, string outputFolderPath)
    {
        
    }

    public async Task<bool> tryParseTables(List<ISpreadsheetDataSource> spreadsheetSources, List<TableData> allTables)
    {
        List<SpreadsheetPageData> allPages = new List<SpreadsheetPageData>();

        foreach (ISpreadsheetDataSource spreadsheetSource in spreadsheetSources)
        {
            List<SpreadsheetPageData> pages = await spreadsheetSource.GetAllSheetsDataAsync();
            allPages.AddRange(pages);
        }
        
        foreach (var page in allPages)
        {
            if (page.name.StartsWith('\'')) {
                continue;
            }
            
            if (TableDataUtilities.ExtractTablesFromPage(page.name, page.values, out List<TableData> resultTables)) {
                allTables.AddRange(resultTables);
            }
        }
        
        AvailableTypes availableTypes = new AvailableTypes();
        availableTypes.RegisterDefaultTypes();

        foreach (var tableData in allTables)
        {
            switch (tableData)
            {
                case DatabaseTableData databaseTableData:
                    availableTypes.Register(new DatabaseTableTypeDescriptor(databaseTableData));
                    break;
                
                case ConstantTableData constantTableData:
                    availableTypes.Register(new ConstantTableTypeDescriptor(constantTableData));
                    break;
            }
        }
        
        bool hasAnyInvalid = false;

        foreach (var tableData in allTables)
        {
            bool isValid = TableDataUtilities.ValidateTableTypesAndValues(tableData, availableTypes);
        
            if (!isValid)
            {
                hasAnyInvalid = true;
            }
        }

        return !hasAnyInvalid;
    }
}