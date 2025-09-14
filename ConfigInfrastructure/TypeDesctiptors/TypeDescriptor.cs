using System;

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
    }
}