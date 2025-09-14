using System;
using System.Reflection;
using ConfigGenerator.ConfigInfrastructure.Data;
using ConfigGenerator.ConfigInfrastructure.TypeDesctiptors;

namespace ConfigGenerator.ConfigInfrastructure
{
    public class ValueConfigTable : IConfigTable
    {
        private enum InitType
        {
            Default,
            Post
        }

        private void InitializeInternal(TableData tableData, AvailableTypes availableTypes, InitType initType)
        {
            if (tableData is not ValueTableData valueTableData)
            {
                throw new Exception($"TableData must be of type {typeof(ValueTableData)}");
            }

            Type currentType = GetType();
        
            foreach (var dataValue in valueTableData.DataValues)
            {
                PropertyInfo? property = currentType.GetProperty(dataValue.Id);

                if (property == null)
                {
                    throw new Exception($"Not found property \"{dataValue.Id}\" in type: {currentType}");
                }

                Type tableType = typeof(IConfigTableItem);
                bool isTableType = tableType.IsAssignableFrom(property.PropertyType)
                                   || tableType.MakeArrayType().IsAssignableFrom(property.PropertyType);
            
                switch (initType)
                {
                    case InitType.Default when isTableType:
                    case InitType.Post when !isTableType:
                        continue;
                }

                TypeDescriptor? typeDescriptor = availableTypes.GetTypeDescriptor(dataValue.Type);
                
                if (typeDescriptor == null)
                {
                    throw new Exception($"Type is not valid: {dataValue.Type}");
                }

                if (!typeDescriptor.Parse(dataValue.Value, out var parsedValue))
                {
                    throw new Exception($"Cannot parse value \"{dataValue.Value}\" of type: {dataValue.Type}");
                }
            
                property.SetValue(this, parsedValue);
            }
        }
    
        public void Initialize(TableData tableData, AvailableTypes availableTypes)
        {
            InitializeInternal(tableData, availableTypes, InitType.Default);
        }

        public void PostInitialize(TableData tableData, AvailableTypes availableTypes)
        {
            InitializeInternal(tableData, availableTypes, InitType.Post);
        }
    }
}