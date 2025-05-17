using System;

namespace ConfigGenerator.ConfigInfrastructure.TypeDesctiptors;

public class DaysTypeDescriptor : FloatTypeDescriptor
{
    public DaysTypeDescriptor() : base("days", nameof(TimeSpan)) { }

    public override object? Parse(string value)
    {
        var parsedValue = base.Parse(value);

        if (parsedValue == null)
        {
            return TimeSpan.Zero;
        }

        return TimeSpan.FromDays((float)parsedValue);
    }
}