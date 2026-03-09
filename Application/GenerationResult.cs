using System.Collections.Generic;
using ConfigGenerator.Common;

namespace ConfigGenerator.Application;

public sealed class GenerationMessage : IOperationMessage
{
    public MessageSeverity Severity { get; }
    public string Step { get; }
    public string Message { get; }
    public string? OutputPath { get; }

    public bool IsError => Severity == MessageSeverity.Error;

    public GenerationMessage(MessageSeverity severity, string step, string message, string? outputPath = null)
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
        Add(new GenerationMessage(MessageSeverity.Info, step, message, outputPath));
    }

    public void AddWarning(string step, string message, string? outputPath = null)
    {
        Add(new GenerationMessage(MessageSeverity.Warning, step, message, outputPath));
    }

    public void AddError(string step, string message, string? outputPath = null)
    {
        Add(new GenerationMessage(MessageSeverity.Error, step, message, outputPath));
    }

    public void Merge(GenerationResult another)
    {
        MergeFrom(another);
    }
}
