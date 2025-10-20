using System;
using ConfigGenerator.ConfigInfrastructure.Data;
using ConfigGenerator.ConfigInfrastructure.Utils;

namespace ConfigGenerator.ConfigInfrastructure.TypeDesctiptors
{
    class ConstantTableTypeDescriptor : IntTypeDescriptor
    {
        private readonly ConstantTableData _tableData;

        public ConstantTableTypeDescriptor(ConstantTableData tableData)
            : base(tableData.Name, "int", typeof(int))
        {
            _tableData = tableData;
        }

        public override bool Parse(string value, out object? result)
        {
            result = 0;
        
            if (string.IsNullOrWhiteSpace(value)) {
                return false;
            }

            value = TableDataUtilities.ExtractFieldName(value);

            foreach (ConstantTableDataItem item in _tableData.Items)
            {
                if (string.Equals(value, item.Name, StringComparison.InvariantCultureIgnoreCase)) {
                    result = item.Value;
                    return true;
                }
            }

            return false;
        }
    }
}