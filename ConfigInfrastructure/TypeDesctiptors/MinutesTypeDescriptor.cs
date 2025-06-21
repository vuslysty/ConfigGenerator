using System;

namespace ConfigGenerator.ConfigInfrastructure.TypeDesctiptors;

public class MinutesTypeDescriptor : FloatTypeDescriptor
{
    public MinutesTypeDescriptor() : base("minutes", nameof(TimeSpan), typeof(TimeSpan)) { }

    public override bool Parse(string value, out object? result)
    {
        result = null;
        
        if (base.Parse(value, out var parsedFloat))
        {
            result = TimeSpan.FromMinutes((float)parsedFloat);
            return true;       
        }

        return false;
    }
}