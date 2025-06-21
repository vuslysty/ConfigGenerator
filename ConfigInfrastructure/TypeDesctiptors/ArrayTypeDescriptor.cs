using System;
using System.Globalization;
using System.IO;
using CsvHelper;
using CsvHelper.Configuration;

namespace ConfigGenerator.ConfigInfrastructure.TypeDesctiptors;

public class ArrayTypeDescriptor : TypeDescriptor
{
    private readonly TypeDescriptor _typeDescriptor;
    
    public ArrayTypeDescriptor(TypeDescriptor typeDescriptor)
        : base(
            $"{typeDescriptor.TypeName}[]", 
            $"{typeDescriptor.RealTypeName}[]",
            typeDescriptor.Type.MakeArrayType())
    {
        _typeDescriptor = typeDescriptor;
    }

    public override bool Parse(string value, out object? arrayResult)
    {
        arrayResult = null;

        if (string.IsNullOrWhiteSpace(value))
        {
            if (_typeDescriptor.Parse(value, out object? result))
            {
                arrayResult = Array.CreateInstance(result.GetType(), 0);
                return true;
            }

            return false;
        }
        
        if (_typeDescriptor.TypeName == "string")
        {
            CsvConfiguration configuration = new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                TrimOptions = TrimOptions.Trim
            };
            
            using var reader = new StringReader(value);
            using var csv = new CsvReader(reader, configuration);

            if (csv.Read())
            {
                arrayResult = csv.Parser.Record;
                return true;
            }

            return false;
        }

        string[] values = value.Split(',');

        Array array;

        try
        {
            array = Array.CreateInstance(_typeDescriptor.Type, values.Length);
        }
        catch
        {
            return false;
        }
        
        
        for (var i = 0; i < values.Length; i++)
        {
            var splitValue = values[i];
            string strValue = splitValue.Trim();

            if (_typeDescriptor.Parse(strValue, out var result))
            {
                array.SetValue(result, i);
            }
            else
            {
                Console.WriteLine($"Error in parsing of array: {TypeName}. Can't parse value: {strValue}");
                return false;
            }
        }
        
        arrayResult = array;
        return true;
    }
}