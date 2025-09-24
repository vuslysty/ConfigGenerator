using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace ConfigGenerator.ConfigInfrastructure.TypeDesctiptors
{
    public class ArrayTypeDescriptor : TypeDescriptor
    {
        private readonly TypeDescriptor _typeDescriptor;
        public static string MagicDelimiter = "#DLMTR#";
    
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

            var values = Tokenize(value, MagicDelimiter);

            Array array;

            try {
                array = Array.CreateInstance(_typeDescriptor.Type, values.Length);
            } catch {
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
        
        private static readonly Regex ArrayTypeRegex = new Regex(
            @"^(.+?)\[(.*)\]$", 
            RegexOptions.Compiled
        );
        
        public static bool IsArrayType(string typeName, out string specialDelimiter, out string cleanTypeName)
        {
            specialDelimiter = null;
            cleanTypeName = null;
    
            if (string.IsNullOrEmpty(typeName)) {
                return false;
            }

            Match match = ArrayTypeRegex.Match(typeName);
    
            if (!match.Success) {
                return false;
            }

            string baseType = match.Groups[1].Value;
            string bracketContent = match.Groups[2].Value;
    
            cleanTypeName = baseType + "[]";
    
            if (string.IsNullOrEmpty(bracketContent)) {
                return true;
            }
    
            specialDelimiter = bracketContent;
            return true;
        }
        
        public static string[] Tokenize(string input, string delimiter = null)
        {
            string[] parts;
            if (!string.IsNullOrEmpty(delimiter)) {
                parts = Regex.Split(input, @"\s*" + Regex.Escape(delimiter) + @"\s*");
            } else {
                parts = new[] { input };
            }
    
            var pattern = @"""(?<content>(?:\\""|.)*?)""|\S+";
            var tokens = new List<string>();
    
            foreach (var part in parts) {
                if (string.IsNullOrWhiteSpace(part)) continue;
        
                var matches = Regex.Matches(part, pattern);
                tokens.AddRange(matches.Select(m => m.Value));
            }
    
            return tokens.ToArray();
        }

        public static string ConvertValuesToSerializedString(string valuesAsString, string delimiter) {
            string resultValue = string.Empty;

            if (valuesAsString == null) {
                return resultValue;
            }
            
            string[] tokens = Tokenize(valuesAsString, delimiter);

            for (var i = 0; i < tokens.Length; i++) {
                string token = tokens[i];

                if (i > 0) {
                    resultValue += MagicDelimiter;
                }
                
                resultValue += token;
            }

            return resultValue;
        }
        
        public static string ConvertValuesToSerializedString(List<string> values) {
            string resultValue = string.Empty;
            
            if (values == null) {
                return resultValue;
            }
            
            for (int i = 0; i < values.Count; i++) {
                if (!string.IsNullOrWhiteSpace(resultValue)) {
                    resultValue += MagicDelimiter;
                }
                
                resultValue += values[i];
            }
            
            return resultValue;
        }
    }
}