using System;

namespace ConfigGenerator.ConfigInfrastructure.TypeDesctiptors;

public class SecondsTypeDescriptor : FloatTypeDescriptor
{
    public SecondsTypeDescriptor() : base("seconds", nameof(TimeSpan)) { }

    public override object? Parse(string value)
    {
        var parsedValue = base.Parse(value);

        if (parsedValue == null)
        {
            return TimeSpan.Zero;
        }

        return TimeSpan.FromSeconds((float)parsedValue);
    }
}