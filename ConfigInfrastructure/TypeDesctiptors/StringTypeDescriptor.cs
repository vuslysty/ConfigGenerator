namespace ConfigGenerator.ConfigInfrastructure.TypeDesctiptors;

public class StringTypeDescriptor : TypeDescriptor
{
    public StringTypeDescriptor() : base("string", TypeKind.Reference) { }

    public override object? Parse(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }
        
        return value;
    }
}