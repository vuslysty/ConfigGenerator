using System;

namespace ConfigGenerator.ConfigInfrastructure.TypeDesctiptors
{
    public class DaysTypeDescriptor : FloatTypeDescriptor
    {
        public DaysTypeDescriptor() : base("days", nameof(TimeSpan), typeof(TimeSpan))
        {
        }

        public override bool Parse(string value, out object? result)
        {
            result = null;
        
            if (base.Parse(value, out var parsedFloat))
            {
                result = TimeSpan.FromDays((float)parsedFloat);
                return true;       
            }

            return false;
        }
    }
}