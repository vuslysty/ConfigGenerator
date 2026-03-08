using System.Collections.Generic;
using System.Threading.Tasks;
using ConfigGenerator.ConfigInfrastructure.Data;
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

    public async Task<ParsedTablesResult> LoadAndValidateDetailedAsync(List<ISpreadsheetDataSource> spreadsheetSources)
    {
        List<TableData> tables = await _tableParser.ParseAsync(spreadsheetSources);
        var validationResult = _tableValidator.Validate(tables);

        return new ParsedTablesResult(tables, validationResult);
    }

    public async Task<List<TableData>> LoadAndValidateAsync(List<ISpreadsheetDataSource> spreadsheetSources)
    {
        ParsedTablesResult result = await LoadAndValidateDetailedAsync(spreadsheetSources);
        return result.ValidationResult.IsValid ? result.Tables : new List<TableData>();
    }

    public async Task<PipelineRunResult> GenerateDetailedAsync(List<ISpreadsheetDataSource> spreadsheetSources, string outputFolderPath)
    {
        ParsedTablesResult parsed = await LoadAndValidateDetailedAsync(spreadsheetSources);
        if (!parsed.ValidationResult.IsValid)
        {
            GenerationResult generationResult = new GenerationResult();
            generationResult.AddWarning("artifacts", "Generation skipped because validation failed.");
            return new PipelineRunResult(parsed, generationResult);
        }

        GenerationResult generation = _configGenerator.GenerateArtifactsDetailed(parsed.Tables, outputFolderPath);
        return new PipelineRunResult(parsed, generation);
    }

    public async Task<bool> GenerateAsync(List<ISpreadsheetDataSource> spreadsheetSources, string outputFolderPath)
    {
        PipelineRunResult result = await GenerateDetailedAsync(spreadsheetSources, outputFolderPath);
        return result.IsSuccess;
    }

    public PipelineRunResult GenerateFromTablesDetailed(List<TableData> tables, string outputFolderPath)
    {
        var validationResult = _tableValidator.Validate(tables);
        ParsedTablesResult parsed = new ParsedTablesResult(tables, validationResult);
        if (!validationResult.IsValid)
        {
            GenerationResult generationResult = new GenerationResult();
            generationResult.AddWarning("artifacts", "Generation skipped because validation failed.");
            return new PipelineRunResult(parsed, generationResult);
        }

        GenerationResult generation = _configGenerator.GenerateArtifactsDetailed(tables, outputFolderPath);
        return new PipelineRunResult(parsed, generation);
    }

    public bool GenerateFromTables(List<TableData> tables, string outputFolderPath)
    {
        PipelineRunResult result = GenerateFromTablesDetailed(tables, outputFolderPath);
        return result.IsSuccess;
    }
}
