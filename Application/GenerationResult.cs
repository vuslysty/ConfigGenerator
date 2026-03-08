using System.Collections.Generic;
using System.Linq;

namespace ConfigGenerator.Application;

public enum GenerationMessageSeverity
{
    Info,
    Warning,
    Error
}

public sealed class GenerationMessage
{
    public GenerationMessageSeverity Severity { get; }
    public string Step { get; }
    public string Message { get; }
    public string? OutputPath { get; }

    public GenerationMessage(GenerationMessageSeverity severity, string step, string message, string? outputPath = null)
    {
        Severity = severity;
        Step = step;
        Message = message;
        OutputPath = outputPath;
    }
}

public sealed class GenerationResult
{
    private readonly List<GenerationMessage> _messages = new List<GenerationMessage>();

    public IReadOnlyList<GenerationMessage> Messages => _messages;
    public bool IsSuccess => _messages.All(message => message.Severity != GenerationMessageSeverity.Error);

    public void Add(GenerationMessage message)
    {
        _messages.Add(message);
    }

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
        _messages.AddRange(another._messages);
    }
}
