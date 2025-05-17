using System;

namespace ConfigGenerator.ConfigInfrastructure.TypeDesctiptors;

public class HoursTypeDescriptor : FloatTypeDescriptor
{
    public HoursTypeDescriptor() : base("hours", nameof(TimeSpan)) { }

    public override object? Parse(string value)
    {
        var parsedValue = base.Parse(value);

        if (parsedValue == null)
        {
            return TimeSpan.Zero;
        }

        return TimeSpan.FromHours((float)parsedValue);
    }
}