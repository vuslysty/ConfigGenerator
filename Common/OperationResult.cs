using System.Collections.Generic;
using System.Linq;

namespace ConfigGenerator.Common;

public interface IOperationMessage
{
    bool IsError { get; }
}

public abstract class OperationResult<TMessage>
    where TMessage : IOperationMessage
{
    private readonly List<TMessage> _messages = new();

    protected IReadOnlyList<TMessage> MessageItems => _messages;
    public bool IsSuccess => _messages.All(message => !message.IsError);

    public void Add(TMessage message)
    {
        _messages.Add(message);
    }

    protected void MergeFrom(OperationResult<TMessage> another)
    {
        _messages.AddRange(another._messages);
    }
}
