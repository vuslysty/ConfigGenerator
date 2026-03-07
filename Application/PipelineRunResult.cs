using ConfigGenerator.ConfigInfrastructure.Validation;

namespace ConfigGenerator.Application;

public sealed class PipelineRunResult
{
    public ParsedTablesResult ParsedTablesResult { get; }
    public GenerationResult GenerationResult { get; }

    public bool IsSuccess => ParsedTablesResult.ValidationResult.IsValid && GenerationResult.IsSuccess;

    public PipelineRunResult(ParsedTablesResult parsedTablesResult, GenerationResult generationResult)
    {
        ParsedTablesResult = parsedTablesResult;
        GenerationResult = generationResult;
    }
}
