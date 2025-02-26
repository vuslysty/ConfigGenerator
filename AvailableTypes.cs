using System;
using System.Collections.Generic;
using System.Linq;

namespace ConfigGenerator;

public abstract class TypeDescriptor
{
    public string TypeName { get; }

    protected TypeDescriptor(string typeName)
    {
        TypeName = typeName;
    }

    public abstract object? Parse(string value);

    public bool Validate(string value)
    {
        var parsedValue = Parse(value);
        return parsedValue != null;
    }
}

public class IntTypeDescriptor : TypeDescriptor
{
    public IntTypeDescriptor() : base("int") { }

    public override object? Parse(string value)
    {
        int result = 0;
        
        if (string.IsNullOrWhiteSpace(value))
        {
            return result;
        }
        
        return TryParseInt(value, out result) ? result : null;
    }
    
    private bool TryParseInt(string value, out int result)
    {
        if (value.StartsWith("0b", StringComparison.OrdinalIgnoreCase)) // Двійкова
        {
            return int.TryParse(value[2..], System.Globalization.NumberStyles.AllowLeadingWhite,
                       System.Globalization.CultureInfo.InvariantCulture, out result) && 
                   (result = Convert.ToInt32(value[2..], 2)) >= 0;
        }
        if (value.StartsWith("0o", StringComparison.OrdinalIgnoreCase)) // Вісімкова
        {
            return int.TryParse(value[2..], System.Globalization.NumberStyles.AllowLeadingWhite,
                       System.Globalization.CultureInfo.InvariantCulture, out result) &&
                   (result = Convert.ToInt32(value[2..], 8)) >= 0;
        }
        if (value.StartsWith("0x", StringComparison.OrdinalIgnoreCase) || value.StartsWith("#")) // Шістнадцяткова (0x... або #...)
        {
            string hexValue = value.StartsWith("#") ? value[1..] : value[2..];
            return int.TryParse(hexValue, System.Globalization.NumberStyles.HexNumber,
                System.Globalization.CultureInfo.InvariantCulture, out result);
        }
        return int.TryParse(value, out result); // Десяткова
    }
}

public class FloatTypeDescriptor : TypeDescriptor
{
    public FloatTypeDescriptor() : base("float") { }

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

public class StringTypeDescriptor : TypeDescriptor
{
    public StringTypeDescriptor() : base("string") { }

    public override object? Parse(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }
        
        return value;
    }
}

public class BoolTypeDescriptor : TypeDescriptor
{
    public BoolTypeDescriptor() : base("bool") { }

    public override object? Parse(string value)
    {
        bool result = false;
        
        if (string.IsNullOrWhiteSpace(value))
        {
            return result;
        }
        
        return bool.TryParse(value, out result) ? result : null;
    }
}

class ValueTableTypeDescriptor : TypeDescriptor
{
    private ValueTableData _tableData;
    
    public ValueTableTypeDescriptor(ValueTableData tableData) : base(tableData.Name)
    {
        _tableData = tableData;
    }
    
    public override object? Parse(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }
        
        foreach (ValueTableDataItem dataValue in _tableData.DataValues)
        {
            if (dataValue.Id == value)
            {
                return dataValue;
            }
        }
        
        return null;
    }
}

class DatabaseTableTypeDescriptor : TypeDescriptor
{
    private DatabaseTableData _tableData;
    
    public DatabaseTableTypeDescriptor(DatabaseTableData tableData) : base(tableData.Name)
    {
        _tableData = tableData;
    }
    
    public override object? Parse(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }
        
        foreach (DatabaseTableValuesLineData valueData in _tableData.ValueLines)
        {
            if (value == valueData.Id)
            {
                return valueData;
            }
        }
        
        return null;
    }
}

public class AvailableTypes
{
    public static readonly IntTypeDescriptor Int = new();
    public static readonly FloatTypeDescriptor Float = new();
    public static readonly StringTypeDescriptor String = new();
    public static readonly BoolTypeDescriptor Bool = new();
    
    private readonly List<TypeDescriptor> _types = new();

    public bool Register(TypeDescriptor type)
    {
        if (GetTypeDescriptor(type.TypeName) != null)
        {
            Console.WriteLine($"Warning: Type \"{type.TypeName}\" is already registered.");
            return false;
        }
        
        _types.Add(type);
        return true;
    }

    public void RegisterDefaultTypes()
    {
        Register(Int);
        Register(Float);
        Register(String);
        Register(Bool);
    }

    public TypeDescriptor? GetTypeDescriptor(string typeName) =>
        _types.FirstOrDefault(t => t.TypeName == typeName);

    public object? ParseValue(string typeName, string value)
    {
        var type = GetTypeDescriptor(typeName);
        return type?.Parse(value);
    }
}