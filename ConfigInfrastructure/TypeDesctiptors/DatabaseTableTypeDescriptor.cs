using ConfigGenerator.ConfigInfrastructure.Data;

namespace ConfigGenerator.ConfigInfrastructure.TypeDesctiptors
{
    class DatabaseTableTypeDescriptor : TypeDescriptor
    {
        private readonly DatabaseTableData _tableData;
    
        public DatabaseTableTypeDescriptor(DatabaseTableData tableData) 
            : base(tableData.Name, $"{tableData.Name}.Item", typeof(object))
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

            foreach (DataObject dataObject in _tableData.DataObjects)
            {
                string id = dataObject.Fields[0].Values[0];
                
                if (id == value) {
                    result = dataObject;
                    return true;
                }
            }
        
            return false;
        }
    }
}