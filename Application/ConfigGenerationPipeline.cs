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

    public async Task<PipelineRunResult> ParseAndValidateDetailedAsync(List<ISpreadsheetDataSource> spreadsheetSources)
    {
        List<TableData> tables = await ParseTablesAsync(spreadsheetSources);
        return ValidateParsedTables(tables);
    }

    public async Task<PipelineRunResult> LoadAndValidateDetailedAsync(List<ISpreadsheetDataSource> spreadsheetSources)
    {
        return await ParseAndValidateDetailedAsync(spreadsheetSources);
    }

    public async Task<List<TableData>> LoadAndValidateAsync(List<ISpreadsheetDataSource> spreadsheetSources)
    {
        PipelineRunResult result = await ParseAndValidateDetailedAsync(spreadsheetSources);
        return result.ValidationResult.IsValid ? result.Tables : new List<TableData>();
    }

    public async Task<PipelineRunResult> GenerateDetailedAsync(List<ISpreadsheetDataSource> spreadsheetSources, string outputFolderPath)
    {
        return await GenerateArtifactsDetailedAsync(spreadsheetSources, outputFolderPath);
    }

    public async Task<PipelineRunResult> GenerateArtifactsDetailedAsync(List<ISpreadsheetDataSource> spreadsheetSources, string outputFolderPath)
    {
        PipelineRunResult parsed = await ParseAndValidateDetailedAsync(spreadsheetSources);
        return GenerateArtifactsFromValidated(parsed, outputFolderPath);
    }

    public async Task<PipelineRunResult> GenerateCodeDetailedAsync(List<ISpreadsheetDataSource> spreadsheetSources, string outputFolderPath)
    {
        PipelineRunResult parsed = await ParseAndValidateDetailedAsync(spreadsheetSources);
        return GenerateCodeFromValidated(parsed, outputFolderPath);
    }

    public async Task<PipelineRunResult> GenerateJsonDetailedAsync(List<ISpreadsheetDataSource> spreadsheetSources, string outputFolderPath)
    {
        PipelineRunResult parsed = await ParseAndValidateDetailedAsync(spreadsheetSources);
        return GenerateJsonFromValidated(parsed, outputFolderPath);
    }

    public async Task<bool> GenerateAsync(List<ISpreadsheetDataSource> spreadsheetSources, string outputFolderPath)
    {
        PipelineRunResult result = await GenerateArtifactsDetailedAsync(spreadsheetSources, outputFolderPath);
        return result.IsSuccess;
    }

    public PipelineRunResult GenerateFromTablesDetailed(List<TableData> tables, string outputFolderPath)
    {
        return GenerateArtifactsFromValidated(ValidateParsedTables(tables), outputFolderPath);
    }

    public PipelineRunResult GenerateCodeFromTablesDetailed(List<TableData> tables, string outputFolderPath)
    {
        return GenerateCodeFromValidated(ValidateParsedTables(tables), outputFolderPath);
    }

    public PipelineRunResult GenerateJsonFromTablesDetailed(List<TableData> tables, string outputFolderPath)
    {
        return GenerateJsonFromValidated(ValidateParsedTables(tables), outputFolderPath);
    }

    public bool GenerateFromTables(List<TableData> tables, string outputFolderPath)
    {
        PipelineRunResult result = GenerateFromTablesDetailed(tables, outputFolderPath);
        return result.IsSuccess;
    }

    private PipelineRunResult ValidateParsedTables(List<TableData> tables)
    {
        ValidationResult validationResult = ValidateTables(tables);
        return new PipelineRunResult(tables, validationResult);
    }

    private PipelineRunResult GenerateArtifactsFromValidated(PipelineRunResult validated, string outputFolderPath)
    {
        if (!validated.ValidationResult.IsValid)
        {
            return CreateValidationFailedRunResult(validated);
        }

        GenerationResult generation = _configGenerator.GenerateArtifactsDetailed(validated.Tables, outputFolderPath);
        return new PipelineRunResult(validated.Tables, validated.ValidationResult, generation);
    }

    private PipelineRunResult GenerateCodeFromValidated(PipelineRunResult validated, string outputFolderPath)
    {
        if (!validated.ValidationResult.IsValid)
        {
            return CreateValidationFailedRunResult(validated);
        }

        GenerationResult generation = _configGenerator.GenerateCodeDetailed(validated.Tables, outputFolderPath);
        return new PipelineRunResult(validated.Tables, validated.ValidationResult, generation);
    }

    private PipelineRunResult GenerateJsonFromValidated(PipelineRunResult validated, string outputFolderPath)
    {
        if (!validated.ValidationResult.IsValid)
        {
            return CreateValidationFailedRunResult(validated);
        }

        GenerationResult generation = _configGenerator.GenerateJsonDetailed(validated.Tables, outputFolderPath);
        return new PipelineRunResult(validated.Tables, validated.ValidationResult, generation);
    }

    private static PipelineRunResult CreateValidationFailedRunResult(PipelineRunResult validated)
    {
        GenerationResult generationResult = new GenerationResult();
        generationResult.AddWarning("artifacts", "Generation skipped because validation failed.");
        return new PipelineRunResult(validated.Tables, validated.ValidationResult, generationResult);
    }
}
