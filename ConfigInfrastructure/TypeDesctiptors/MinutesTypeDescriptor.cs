using System;

namespace ConfigGenerator.ConfigInfrastructure.TypeDesctiptors;

public class MinutesTypeDescriptor : FloatTypeDescriptor
{
    public MinutesTypeDescriptor() : base("minutes", nameof(TimeSpan)) { }

    public override object? Parse(string value)
    {
        var parsedValue = base.Parse(value);

        if (parsedValue == null)
        {
            return TimeSpan.Zero;
        }

        return TimeSpan.FromMinutes((float)parsedValue);
    }
}