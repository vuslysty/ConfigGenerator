using System;

namespace ConfigGenerator.ConfigInfrastructure.TypeDesctiptors
{
    public class SecondsTypeDescriptor : FloatTypeDescriptor
    {
        public SecondsTypeDescriptor() : base("seconds", nameof(TimeSpan), typeof(TimeSpan)) { }

        public override bool Parse(string value, out object? result)
        {
            result = null;
        
            if (base.Parse(value, out var parsedFloat))
            {
                result = TimeSpan.FromSeconds((float)parsedFloat);
                return true;       
            }

            return false;
        }
    }
}