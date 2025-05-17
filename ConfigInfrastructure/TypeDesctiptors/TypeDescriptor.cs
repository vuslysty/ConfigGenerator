namespace ConfigGenerator.ConfigInfrastructure.TypeDesctiptors;

public abstract class TypeDescriptor
{
    public string TypeName { get; }
    public string RealTypeName { get; }

    protected TypeDescriptor(string typeName, string realTypeName)
    {
        TypeName = typeName;
        RealTypeName = realTypeName;
    }
    
    protected TypeDescriptor(string typeName)
    {
        TypeName = typeName;
        RealTypeName = typeName;
    }

    public abstract bool Parse(string value, out object? result);
}