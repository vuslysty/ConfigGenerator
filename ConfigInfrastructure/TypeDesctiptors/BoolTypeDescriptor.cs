namespace ConfigGenerator.ConfigInfrastructure.TypeDesctiptors;

public class  BoolTypeDescriptor : TypeDescriptor
{
    public BoolTypeDescriptor() : base("bool") { }

    public override bool Parse(string value, out object? result)
    {
        result = false;
        
        if (string.IsNullOrWhiteSpace(value))
        {
            return true;
        }

        if (bool.TryParse(value, out var res))
        {
            result = res;
            return true;
        }

        return false;
    }
}