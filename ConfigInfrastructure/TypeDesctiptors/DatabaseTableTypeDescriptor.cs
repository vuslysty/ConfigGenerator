namespace ConfigGenerator.ConfigInfrastructure.TypeDesctiptors;

class DatabaseTableTypeDescriptor : TypeDescriptor
{
    private readonly DatabaseTableData _tableData;
    
    public DatabaseTableTypeDescriptor(DatabaseTableData tableData) 
        : base(tableData.Name, $"{tableData.Name}.Item", TypeKind.Reference)
    {
        _tableData = tableData;
    }
    
    public override object? Parse(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }
        
        foreach (DatabaseTableValuesLineData valueData in _tableData.ValueLines)
        {
            if (value == valueData.Id)
            {
                return valueData;
            }
        }
        
        return null;
    }
}