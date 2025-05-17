namespace ConfigGenerator.ConfigInfrastructure.TypeDesctiptors;

public class  BoolTypeDescriptor : TypeDescriptor
{
    public BoolTypeDescriptor() : base("bool") { }

    public override object? Parse(string value)
    {
        bool result = false;
        
        if (string.IsNullOrWhiteSpace(value))
        {
            return result;
        }
        
        return bool.TryParse(value, out result) ? result : null;
    }
}