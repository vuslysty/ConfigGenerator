using System.Collections.Generic;
using ConfigGenerator.ConfigInfrastructure.Data;
using ConfigGenerator.ConfigInfrastructure.Validation;

namespace ConfigGenerator.Application;

public sealed class PipelineRunResult
{
    public List<TableData> Tables { get; }
    public ValidationResult ValidationResult { get; }
    public GenerationResult GenerationResult { get; }

    public bool IsSuccess => ValidationResult.IsValid && GenerationResult.IsSuccess;

    public PipelineRunResult(
        List<TableData> tables,
        ValidationResult validationResult,
        GenerationResult? generationResult = null)
    {
        Tables = tables;
        ValidationResult = validationResult;
        GenerationResult = generationResult ?? new GenerationResult();
    }
}
