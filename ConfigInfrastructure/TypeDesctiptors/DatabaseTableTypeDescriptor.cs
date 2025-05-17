namespace ConfigGenerator.ConfigInfrastructure.TypeDesctiptors;

class DatabaseTableTypeDescriptor : TypeDescriptor
{
    private readonly DatabaseTableData _tableData;
    
    public DatabaseTableTypeDescriptor(DatabaseTableData tableData) 
        : base(tableData.Name, $"{tableData.Name}.Item")
    {
        _tableData = tableData;
    }
    
    public override bool Parse(string value, out object? result)
    {
        result = null;
        
        if (string.IsNullOrWhiteSpace(value))
        {
            return true;
        }
        
        foreach (DatabaseTableValuesLineData? valueData in _tableData.ValueLines)
        {
            if (value == valueData.Id)
            {
                result = valueData;
                return true;
            }
        }
        
        return false;
    }
}