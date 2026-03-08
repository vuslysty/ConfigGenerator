using System.Collections.Generic;
using System.Threading.Tasks;
using ConfigGenerator.ConfigInfrastructure.Data;
using ConfigGenerator.ConfigInfrastructure.Validation;
using ConfigGenerator.Parsing;
using ConfigGenerator.Spreadsheet;

namespace ConfigGenerator.Application;

public sealed class ConfigGenerationPipeline
{
    private readonly ConfigGenerator _configGenerator;
    private readonly ITableParser _tableParser;
    private readonly ITableValidator _tableValidator;

    public ConfigGenerationPipeline(
        ConfigGenerator configGenerator,
        ITableParser tableParser,
        ITableValidator tableValidator)
    {
        _configGenerator = configGenerator;
        _tableParser = tableParser;
        _tableValidator = tableValidator;
    }

    public async Task<List<TableData>> ParseTablesAsync(List<ISpreadsheetDataSource> spreadsheetSources)
    {
        return await _tableParser.ParseAsync(spreadsheetSources);
    }

    public ValidationResult ValidateTables(List<TableData> tables)
    {
        return _tableValidator.Validate(tables);
    }

    public async Task<ParsedTablesResult> ParseAndValidateDetailedAsync(List<ISpreadsheetDataSource> spreadsheetSources)
    {
        List<TableData> tables = await ParseTablesAsync(spreadsheetSources);
        ValidationResult validationResult = ValidateTables(tables);

        return new ParsedTablesResult(tables, validationResult);
    }

    public async Task<ParsedTablesResult> LoadAndValidateDetailedAsync(List<ISpreadsheetDataSource> spreadsheetSources)
    {
        return await ParseAndValidateDetailedAsync(spreadsheetSources);
    }

    public async Task<List<TableData>> LoadAndValidateAsync(List<ISpreadsheetDataSource> spreadsheetSources)
    {
        ParsedTablesResult result = await ParseAndValidateDetailedAsync(spreadsheetSources);
        return result.ValidationResult.IsValid ? result.Tables : new List<TableData>();
    }

    public async Task<PipelineRunResult> GenerateDetailedAsync(List<ISpreadsheetDataSource> spreadsheetSources, string outputFolderPath)
    {
        return await GenerateArtifactsDetailedAsync(spreadsheetSources, outputFolderPath);
    }

    public async Task<PipelineRunResult> GenerateArtifactsDetailedAsync(List<ISpreadsheetDataSource> spreadsheetSources, string outputFolderPath)
    {
        ParsedTablesResult parsed = await ParseAndValidateDetailedAsync(spreadsheetSources);
        return GenerateArtifactsFromParsed(parsed, outputFolderPath);
    }

    public async Task<PipelineRunResult> GenerateCodeDetailedAsync(List<ISpreadsheetDataSource> spreadsheetSources, string outputFolderPath)
    {
        ParsedTablesResult parsed = await ParseAndValidateDetailedAsync(spreadsheetSources);
        return GenerateCodeFromParsed(parsed, outputFolderPath);
    }

    public async Task<PipelineRunResult> GenerateJsonDetailedAsync(List<ISpreadsheetDataSource> spreadsheetSources, string outputFolderPath)
    {
        ParsedTablesResult parsed = await ParseAndValidateDetailedAsync(spreadsheetSources);
        return GenerateJsonFromParsed(parsed, outputFolderPath);
    }

    public async Task<bool> GenerateAsync(List<ISpreadsheetDataSource> spreadsheetSources, string outputFolderPath)
    {
        PipelineRunResult result = await GenerateArtifactsDetailedAsync(spreadsheetSources, outputFolderPath);
        return result.IsSuccess;
    }

    public PipelineRunResult GenerateFromTablesDetailed(List<TableData> tables, string outputFolderPath)
    {
        return GenerateArtifactsFromParsed(ValidateParsedTables(tables), outputFolderPath);
    }

    public PipelineRunResult GenerateCodeFromTablesDetailed(List<TableData> tables, string outputFolderPath)
    {
        return GenerateCodeFromParsed(ValidateParsedTables(tables), outputFolderPath);
    }

    public PipelineRunResult GenerateJsonFromTablesDetailed(List<TableData> tables, string outputFolderPath)
    {
        return GenerateJsonFromParsed(ValidateParsedTables(tables), outputFolderPath);
    }

    public bool GenerateFromTables(List<TableData> tables, string outputFolderPath)
    {
        PipelineRunResult result = GenerateFromTablesDetailed(tables, outputFolderPath);
        return result.IsSuccess;
    }

    private ParsedTablesResult ValidateParsedTables(List<TableData> tables)
    {
        ValidationResult validationResult = ValidateTables(tables);
        return new ParsedTablesResult(tables, validationResult);
    }

    private PipelineRunResult GenerateArtifactsFromParsed(ParsedTablesResult parsed, string outputFolderPath)
    {
        if (!parsed.ValidationResult.IsValid)
        {
            return CreateValidationFailedRunResult(parsed);
        }

        GenerationResult generation = _configGenerator.GenerateArtifactsDetailed(parsed.Tables, outputFolderPath);
        return new PipelineRunResult(parsed, generation);
    }

    private PipelineRunResult GenerateCodeFromParsed(ParsedTablesResult parsed, string outputFolderPath)
    {
        if (!parsed.ValidationResult.IsValid)
        {
            return CreateValidationFailedRunResult(parsed);
        }

        GenerationResult generation = _configGenerator.GenerateCodeDetailed(parsed.Tables, outputFolderPath);
        return new PipelineRunResult(parsed, generation);
    }

    private PipelineRunResult GenerateJsonFromParsed(ParsedTablesResult parsed, string outputFolderPath)
    {
        if (!parsed.ValidationResult.IsValid)
        {
            return CreateValidationFailedRunResult(parsed);
        }

        GenerationResult generation = _configGenerator.GenerateJsonDetailed(parsed.Tables, outputFolderPath);
        return new PipelineRunResult(parsed, generation);
    }

    private static PipelineRunResult CreateValidationFailedRunResult(ParsedTablesResult parsed)
    {
        GenerationResult generationResult = new GenerationResult();
        generationResult.AddWarning("artifacts", "Generation skipped because validation failed.");
        return new PipelineRunResult(parsed, generationResult);
    }
}
