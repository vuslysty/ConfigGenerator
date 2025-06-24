namespace ConfigGenerator.ConfigInfrastructure.TypeDesctiptors;

public class IntTypeDescriptor : TypeDescriptor
{
    public IntTypeDescriptor() : base("int", typeof(int)) { }

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