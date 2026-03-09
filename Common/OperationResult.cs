using System.Collections.Generic;

namespace ConfigGenerator.Common;

public sealed class OperationMessage
{
    public MessageSeverity Severity { get; }
    public string Message { get; }

    public string? TableName { get; }
    public int? Row { get; }
    public string? Column { get; }

    public string? Step { get; }

    public bool IsError => Severity == MessageSeverity.Error;

    public OperationMessage(
        MessageSeverity severity,
        string message,
        string? tableName = null,
        int? row = null,
        string? column = null,
        string? step = null)
    {
        Severity = severity;
        Message = message;
        TableName = tableName;
        Row = row;
        Column = column;
        Step = step;
    }
}

public sealed class OperationResult
{
    private readonly List<OperationMessage> _messages = new();
    public string Identifier { get; }

    public IReadOnlyList<OperationMessage> Messages => _messages;

    public bool IsSuccess => _messages.TrueForAll(message => !message.IsError);

    public OperationResult(string identifier = OperationResultIdentifiers.Operation)
    {
        Identifier = identifier;
    }

    public void Add(OperationMessage message)
    {
        _messages.Add(message);
    }

    public void AddInfo(
        string message,
        string? tableName = null,
        int? row = null,
        string? column = null,
        string? step = null)
    {
        Add(new OperationMessage(MessageSeverity.Info, message, tableName, row, column, step));
    }

    public void AddWarning(
        string message,
        string? tableName = null,
        int? row = null,
        string? column = null,
        string? step = null)
    {
        Add(new OperationMessage(MessageSeverity.Warning, message, tableName, row, column, step));
    }

    public void AddError(
        string message,
        string? tableName = null,
        int? row = null,
        string? column = null,
        string? step = null)
    {
        Add(new OperationMessage(MessageSeverity.Error, message, tableName, row, column, step));
    }

    public void Merge(OperationResult another)
    {
        _messages.AddRange(another._messages);
    }
}
