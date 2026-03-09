using System.Collections.Generic;
using ConfigGenerator.Common;
using ConfigGenerator.ConfigInfrastructure.Data;

namespace ConfigGenerator.Application;

public sealed class PipelineRunResult
{
    public List<TableData> Tables { get; }
    public OperationResult ValidationResult { get; }
    public OperationResult GenerationResult { get; }

    public bool IsSuccess => ValidationResult.IsSuccess && GenerationResult.IsSuccess;

    public OperationResult Result
    {
        get
        {
            OperationResult merged = new OperationResult(OperationResultIdentifiers.Pipeline);
            merged.Merge(ValidationResult);
            merged.Merge(GenerationResult);
            return merged;
        }
    }

    public PipelineRunResult(List<TableData> tables, OperationResult validationResult, OperationResult? generationResult = null)
    {
        Tables = tables;
        ValidationResult = validationResult;
        GenerationResult = generationResult ?? new OperationResult(OperationResultIdentifiers.Generator);
    }
}
