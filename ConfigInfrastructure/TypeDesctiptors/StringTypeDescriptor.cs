namespace ConfigGenerator.ConfigInfrastructure.TypeDesctiptors;

public class StringTypeDescriptor : TypeDescriptor
{
    public StringTypeDescriptor() : base("string", typeof(string)) { }

    public override bool Parse(string value, out object? result)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            result = string.Empty;
        }
        else
        {
            result = value;
        }
        
        return true;
    }
}