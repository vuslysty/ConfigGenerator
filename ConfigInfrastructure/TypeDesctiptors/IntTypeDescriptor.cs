using System;

namespace ConfigGenerator.ConfigInfrastructure.TypeDesctiptors
{
    public class IntTypeDescriptor : TypeDescriptor
    {
        public IntTypeDescriptor() : base("int", typeof(int)) { }
        public IntTypeDescriptor(string typeName, Type type) : base(typeName, type) { }
        public IntTypeDescriptor(string typeName, string realTypeName, Type type) : base(typeName, realTypeName, type) { }

        public override bool Parse(string value, out object? result)
        {
            result = 0;
        
            if (string.IsNullOrWhiteSpace(value))
            {
                return true;
            }

            if (ParseUtils.TryParseInt(value, out var res))
            {
                result = res;
                return true;
            }

            return false;
        }
    }
}