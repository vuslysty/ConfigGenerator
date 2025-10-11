using System;
using System.Collections.Generic;
using System.Linq;
using ConfigGenerator.ConfigInfrastructure.TypeDesctiptors;

namespace ConfigGenerator.ConfigInfrastructure
{
    public class AvailableTypes
    {
        public static readonly IntTypeDescriptor Int = new();
        public static readonly FloatTypeDescriptor Float = new();
        public static readonly StringTypeDescriptor String = new();
        public static readonly BoolTypeDescriptor Bool = new();
    
        public static readonly SecondsTypeDescriptor Seconds = new();
        public static readonly MinutesTypeDescriptor Minutes = new();
        public static readonly HoursTypeDescriptor Hours = new();
        public static readonly DaysTypeDescriptor Days = new();
    
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

        public void Initialize() {
            foreach (var type in _types) {
                if (type is IInitializable initializable) {
                    initializable.Initialize();
                }
            }
        }

        public void RegisterDefaultTypes()
        {
            Register(Int);
            Register(Float);
            Register(String);
            Register(Bool);
            Register(Seconds);
            Register(Minutes);
            Register(Hours);
            Register(Days);
        }

        public TypeDescriptor? GetTypeDescriptor(string typeName) =>
            _types.FirstOrDefault(t => string.Equals(t.TypeName, typeName, StringComparison.OrdinalIgnoreCase));

        public bool ParseValue(string typeName, string value, out object? result)
        {
            result = null;
        
            var typeDescriptor = GetTypeDescriptor(typeName);

            if (typeDescriptor == null)
            {
                return false;
            }
        
            return typeDescriptor.Parse(value, out result);
        }

        public bool ParseAsArray(string typeName, List<string> values, out object? arrayResult)
        {
            arrayResult = null;
        
            var typeDescriptor = GetTypeDescriptor(typeName);

            if (typeDescriptor == null)
            {
                return false;
            }
            
            return typeDescriptor.ParseAsArray(values, out arrayResult);
        }
    }
}