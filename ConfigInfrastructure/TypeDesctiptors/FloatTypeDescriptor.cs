using System;

namespace ConfigGenerator.ConfigInfrastructure.TypeDesctiptors;

public class FloatTypeDescriptor : TypeDescriptor
{
    public FloatTypeDescriptor() : base("float", typeof(float)) { }
    
    public FloatTypeDescriptor(string typeName, Type type) : base(typeName, type) { }
    
    public FloatTypeDescriptor(string typeName, string realTypeName, Type type) : base(typeName, realTypeName, type) { }

    public override bool Parse(string value, out object? result)
    {
        result = 0f;
        
        if (string.IsNullOrWhiteSpace(value))
        {
            return true;
        }

        if (value.Contains(','))
        {
            Console.WriteLine("Error: used ',' instead of '.' for floating-point types. Only dot are supported.");
            return false;
        }

        if (float.TryParse(value, out var res))
        {
            result = res;
            return true;
        }
        
        return false;
    }
}