namespace ConfigGenerator.ConfigInfrastructure.TypeDesctiptors;

public enum TypeKind
{
    Value,
    Reference,
}

public abstract class TypeDescriptor
{
    public string TypeName { get; }
    public string RealTypeName { get; }
    public TypeKind TypeKind { get; }

    protected TypeDescriptor(string typeName, string realTypeName, TypeKind typeKind = TypeKind.Value)
    {
        TypeName = typeName;
        RealTypeName = realTypeName;
        TypeKind = typeKind;
    }
    
    protected TypeDescriptor(string typeName, TypeKind typeKind = TypeKind.Value)
    {
        TypeName = typeName;
        RealTypeName = typeName;
        TypeKind = typeKind;
    }

    public abstract object? Parse(string value);
}