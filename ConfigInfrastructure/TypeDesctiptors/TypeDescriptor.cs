using System;
using System.Collections.Generic;

namespace ConfigGenerator.ConfigInfrastructure.TypeDesctiptors
{
    public abstract class TypeDescriptor
    {
        public string TypeName { get; }
        public string RealTypeName { get; }
        public Type Type { get; }

        protected TypeDescriptor(string typeName, string realTypeName, Type type)
        {
            TypeName = typeName;
            RealTypeName = realTypeName;
            Type = type;
        }
    
        protected TypeDescriptor(string typeName, Type type)
        {
            TypeName = typeName;
            RealTypeName = typeName;
            Type = type;
        }

        public abstract bool Parse(string value, out object? result);

        public bool ParseAsArray(List<string> values, out object? arrayResult) {
            arrayResult = null;
            
            if (values == null)
            {
                arrayResult = Array.CreateInstance(Type, 0);
                return true;
            }
            
            Array array;

            try {
                array = Array.CreateInstance(Type, values.Count);
            } catch {
                return false;
            }
            
            for (var i = 0; i < values.Count; i++)
            {
                var splitValue = values[i];
                string strValue = splitValue.Trim();

                if (Parse(strValue, out var result)) {
                    array.SetValue(result, i);
                } else {
                    Console.WriteLine($"Error in parsing of array: {TypeName}. Can't parse value: {strValue}");
                    return false;
                }
            }
        
            arrayResult = array;
            return true;
        }
    }
}