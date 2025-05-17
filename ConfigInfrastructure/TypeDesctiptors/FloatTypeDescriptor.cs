using System;

namespace ConfigGenerator.ConfigInfrastructure.TypeDesctiptors;

public class FloatTypeDescriptor : TypeDescriptor
{
    public FloatTypeDescriptor() : base("float") { }
    
    public FloatTypeDescriptor(string typeName) : base(typeName) { }
    
    public FloatTypeDescriptor(string typeName, string realTypeName) : base(typeName, realTypeName) { }

    public override object? Parse(string value)
    {
        float result = 0;
        
        if (string.IsNullOrWhiteSpace(value))
        {
            return result;
        }

        if (value.Contains(','))
        {
            Console.WriteLine("Error: used ',' instead of '.' for floating-point types. Only dot are supported.");
            return null;
        }
        
        return float.TryParse(value, out result) ? result : null;
    }
}