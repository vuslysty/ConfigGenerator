using System;

namespace ConfigGenerator.ConfigInfrastructure.TypeDesctiptors
{
    public class HoursTypeDescriptor : FloatTypeDescriptor
    {
        public HoursTypeDescriptor() : base("hours", nameof(TimeSpan), typeof(TimeSpan)) { }

        public override bool Parse(string value, out object? result)
        {
            result = null;
        
            if (base.Parse(value, out var parsedFloat))
            {
                result = TimeSpan.FromHours((float)parsedFloat);
                return true;       
            }

            return false;
        }
    }
}