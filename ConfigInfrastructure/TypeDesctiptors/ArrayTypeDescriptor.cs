using System;
using System.Linq;
using System.Text.RegularExpressions;

namespace ConfigGenerator.ConfigInfrastructure.TypeDesctiptors
{
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

            const string basePattern =  @"""(?<content>(?:\\""|.)*?)""|[^\|,\s]+";
            const string floatPattern = @"""(?<content>(?:\\""|.)*?)""|[^\|\s]+";

            var pattern = _typeDescriptor is FloatTypeDescriptor 
                ? floatPattern
                : basePattern;
        
            var matches = Regex.Matches(value, pattern);
        
            var values = matches
                .Select(m => m.Groups["content"].Success 
                    ? m.Groups["content"].Value  // Якщо знайдено в лапках
                    : m.Value)                   // Інакше — беремо як є
                .ToList();

            Array array;

            try
            {
                array = Array.CreateInstance(_typeDescriptor.Type, values.Count);
            }
            catch
            {
                return false;
            }
        
        
            for (var i = 0; i < values.Count; i++)
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
}