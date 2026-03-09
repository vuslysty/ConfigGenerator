using System.Collections.Generic;
using ConfigGenerator.Common;

namespace ConfigGenerator.Application;

public enum GenerationMessageSeverity
{
    Info,
    Warning,
    Error
}

public sealed class GenerationMessage : IOperationMessage
{
    public GenerationMessageSeverity Severity { get; }
    public string Step { get; }
    public string Message { get; }
    public string? OutputPath { get; }

    public bool IsError => Severity == GenerationMessageSeverity.Error;

    public GenerationMessage(GenerationMessageSeverity severity, string step, string message, string? outputPath = null)
    {
        Severity = severity;
        Step = step;
        Message = message;
        OutputPath = outputPath;
    }
}

public sealed class GenerationResult : OperationResult<GenerationMessage>
{
    public IReadOnlyList<GenerationMessage> Messages => MessageItems;

    public void AddInfo(string step, string message, string? outputPath = null)
    {
        Add(new GenerationMessage(GenerationMessageSeverity.Info, step, message, outputPath));
    }

    public void AddWarning(string step, string message, string? outputPath = null)
    {
        Add(new GenerationMessage(GenerationMessageSeverity.Warning, step, message, outputPath));
    }

    public void AddError(string step, string message, string? outputPath = null)
    {
        Add(new GenerationMessage(GenerationMessageSeverity.Error, step, message, outputPath));
    }

    public void Merge(GenerationResult another)
    {
        MergeFrom(another);
    }
}
