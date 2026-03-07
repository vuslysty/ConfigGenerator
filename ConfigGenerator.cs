using System.Collections.Generic;
using System.Threading.Tasks;
using ConfigGenerator.Application;
using ConfigGenerator.Application.Artifacts;
using ConfigGenerator.ConfigInfrastructure;
using ConfigGenerator.ConfigInfrastructure.Data;
using ConfigGenerator.Parsing;
using ConfigGenerator.Spreadsheet;

namespace ConfigGenerator;

public class ConfigGenerator
{
    private readonly ITableDataSerializer _tableDataSerializer;
    private readonly ITypeRegistryFactory _typeRegistryFactory;
    private readonly ITableParser _tableParser;
    private readonly ITableValidator _tableValidator;
    private readonly IArtifactWriter _artifactWriter;
    private readonly string _className;
    private readonly string _namespaceName;

    public ConfigGenerator(ITableDataSerializer tableDataSerializer, string className, string namespaceName)
        : this(tableDataSerializer, className, namespaceName, new TypeRegistryFactory(), new TableParser(), new FileArtifactWriter())
    {
    }

    public ConfigGenerator(
        ITableDataSerializer tableDataSerializer,
        string className,
        string namespaceName,
        ITypeRegistryFactory typeRegistryFactory,
        ITableParser tableParser,
        IArtifactWriter artifactWriter)
    {
        _className = className;
        _namespaceName = namespaceName;
        _tableDataSerializer = tableDataSerializer;
        _typeRegistryFactory = typeRegistryFactory;
        _tableParser = tableParser;
        _tableValidator = new TableValidator(_typeRegistryFactory);
        _artifactWriter = artifactWriter;
    }

    public void GenerateCode(string json, string outputFolderPath)
    {
        List<TableData> allTables = _tableDataSerializer.Deserialize(json);
        GenerateCode(allTables, outputFolderPath);
    }

    public async Task GenerateCodeAsync(List<ISpreadsheetDataSource> spreadsheetSources, string outputFolderPath)
    {
        List<TableData> allTables = await ParseTablesAsync(spreadsheetSources);
        if (!_tableValidator.Validate(allTables).IsValid)
        {
            return;
        }

        GenerateCode(allTables, outputFolderPath);
    }

    public GenerationResult GenerateCodeDetailed(List<TableData> tables, string outputFolderPath)
    {
        GenerationResult result = new GenerationResult();
        result.AddInfo("code", $"Starting code generation for {tables.Count} tables.");

        string code = CodeGenerator.GenerateConfigClasses(tables, _className, _namespaceName, _typeRegistryFactory);
        string filePath = System.IO.Path.Combine(outputFolderPath, $"{_className}.cs");
        _artifactWriter.WriteText(filePath, code);

        result.AddInfo("code", $"Code generated to {filePath}.", filePath);
        return result;
    }

    public void GenerateCode(List<TableData> tables, string outputFolderPath)
    {
        GenerateCodeDetailed(tables, outputFolderPath);
    }

    public async Task GenerateJsonAsync(List<ISpreadsheetDataSource> spreadsheetSources, string outputFolderPath)
    {
        List<TableData> allTables = await ParseTablesAsync(spreadsheetSources);
        if (!_tableValidator.Validate(allTables).IsValid)
        {
            return;
        }

        GenerateJson(allTables, outputFolderPath);
    }

    public GenerationResult GenerateJsonDetailed(List<TableData> tables, string outputFolderPath)
    {
        GenerationResult result = new GenerationResult();
        result.AddInfo("json", $"Starting json generation for {tables.Count} tables.");

        string json = _tableDataSerializer.Serialize(tables);
        string filePath = System.IO.Path.Combine(outputFolderPath, $"{_className}.json");
        _artifactWriter.WriteText(filePath, json);

        result.AddInfo("json", $"Json generated to {filePath}.", filePath);
        return result;
    }

    public void GenerateJson(List<TableData> tables, string outputFolderPath)
    {
        GenerateJsonDetailed(tables, outputFolderPath);
    }

    public GenerationResult GenerateArtifactsDetailed(List<TableData> tables, string outputFolderPath)
    {
        GenerationResult result = new GenerationResult();
        result.AddInfo("artifacts", "Starting full artifact generation.");
        result.Merge(GenerateCodeDetailed(tables, outputFolderPath));
        result.Merge(GenerateJsonDetailed(tables, outputFolderPath));
        result.AddInfo("artifacts", "Full artifact generation completed.");
        return result;
    }

    public async Task<List<TableData>> ParseTablesAsync(List<ISpreadsheetDataSource> spreadsheetSources)
    {
        return await _tableParser.ParseAsync(spreadsheetSources);
    }

    public async Task<bool> TryParseTables(List<ISpreadsheetDataSource> spreadsheetSources, List<TableData> allTables)
    {
        List<TableData> parsedTables = await ParseTablesAsync(spreadsheetSources);
        allTables.AddRange(parsedTables);

        return _tableValidator.Validate(allTables).IsValid;
    }

    [System.Obsolete("Use GenerateCode")]
    public void generateCode(string json, string outputFolderPath) => GenerateCode(json, outputFolderPath);

    [System.Obsolete("Use GenerateCodeAsync")]
    public Task generateCode(List<ISpreadsheetDataSource> spreadsheetSources, string outputFolderPath) =>
        GenerateCodeAsync(spreadsheetSources, outputFolderPath);

    [System.Obsolete("Use GenerateCode")]
    public void generateCode(List<TableData> tables, string outputFolderPath) => GenerateCode(tables, outputFolderPath);

    [System.Obsolete("Use GenerateJsonAsync")]
    public Task generateJson(List<ISpreadsheetDataSource> spreadsheetSources, string outputFolderPath) =>
        GenerateJsonAsync(spreadsheetSources, outputFolderPath);

    [System.Obsolete("Use GenerateJson")]
    public void generateJson(List<TableData> tables, string outputFolderPath) => GenerateJson(tables, outputFolderPath);

    [System.Obsolete("Use TryParseTables")]
    public Task<bool> tryParseTables(List<ISpreadsheetDataSource> spreadsheetSources, List<TableData> allTables) =>
        TryParseTables(spreadsheetSources, allTables);
}
