using System.Collections.Generic;
using System.Threading.Tasks;
using ConfigGenerator.Application;
using ConfigGenerator.Application.Artifacts;
using ConfigGenerator.Common;
using ConfigGenerator.ConfigInfrastructure;
using ConfigGenerator.ConfigInfrastructure.Data;
using ConfigGenerator.Parsing;
using ConfigGenerator.Spreadsheet;

namespace ConfigGenerator;

public class ConfigGenerator
{
    private const string CodeFileExtension = "cs";
    private const string JsonFileExtension = "json";

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
        List<TableData>? allTables = await GetValidatedTablesAsync(spreadsheetSources);

        if (allTables == null)
        {
            return;
        }

        GenerateCode(allTables, outputFolderPath);
    }

    public OperationResult GenerateCodeDetailed(List<TableData> tables, string outputFolderPath)
    {
        OperationResult result = new OperationResult(OperationResultIdentifiers.Generator);
        result.AddInfo($"Starting code generation for {tables.Count} tables.", step: OperationSteps.Generation.Code);

        string code = CodeGenerator.GenerateConfigClasses(tables, _className, _namespaceName, _typeRegistryFactory);
        string filePath = BuildArtifactPath(outputFolderPath, CodeFileExtension);
        _artifactWriter.WriteText(filePath, code);

        result.AddInfo($"Code generated to {filePath}.", step: OperationSteps.Generation.Code);
        return result;
    }

    public void GenerateCode(List<TableData> tables, string outputFolderPath)
    {
        GenerateCodeDetailed(tables, outputFolderPath);
    }

    public async Task GenerateJsonAsync(List<ISpreadsheetDataSource> spreadsheetSources, string outputFolderPath)
    {
        List<TableData>? allTables = await GetValidatedTablesAsync(spreadsheetSources);

        if (allTables == null)
        {
            return;
        }

        GenerateJson(allTables, outputFolderPath);
    }

    public OperationResult GenerateJsonDetailed(List<TableData> tables, string outputFolderPath)
    {
        OperationResult result = new OperationResult(OperationResultIdentifiers.Generator);
        result.AddInfo($"Starting json generation for {tables.Count} tables.", step: OperationSteps.Generation.Json);

        string json = _tableDataSerializer.Serialize(tables);
        string filePath = BuildArtifactPath(outputFolderPath, JsonFileExtension);
        _artifactWriter.WriteText(filePath, json);

        result.AddInfo($"Json generated to {filePath}.", step: OperationSteps.Generation.Json);
        return result;
    }

    public void GenerateJson(List<TableData> tables, string outputFolderPath)
    {
        GenerateJsonDetailed(tables, outputFolderPath);
    }

    public OperationResult GenerateArtifactsDetailed(List<TableData> tables, string outputFolderPath)
    {
        OperationResult result = new OperationResult(OperationResultIdentifiers.Generator);
        result.AddInfo("Starting full artifact generation.", step: OperationSteps.Generation.Artifacts);
        result.Merge(GenerateCodeDetailed(tables, outputFolderPath));
        result.Merge(GenerateJsonDetailed(tables, outputFolderPath));
        result.AddInfo("Full artifact generation completed.", step: OperationSteps.Generation.Artifacts);
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

        return _tableValidator.Validate(allTables).IsSuccess;
    }

    private async Task<List<TableData>?> GetValidatedTablesAsync(List<ISpreadsheetDataSource> spreadsheetSources)
    {
        List<TableData> allTables = await ParseTablesAsync(spreadsheetSources);

        if (!_tableValidator.Validate(allTables).IsSuccess)
        {
            return null;
        }

        return allTables;
    }

    private string BuildArtifactPath(string outputFolderPath, string extension)
    {
        return System.IO.Path.Combine(outputFolderPath, $"{_className}.{extension}");
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
