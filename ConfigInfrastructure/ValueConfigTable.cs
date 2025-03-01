using System;
using System.Reflection;

namespace ConfigGenerator.ConfigInfrastructure;

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

            bool isTableType = typeof(IConfigTableItem<string>).IsAssignableFrom(property.PropertyType);
            
            switch (initType)
            {
                case InitType.Default when isTableType:
                case InitType.Post when !isTableType:
                    continue;
            }
            
            var parsedObject = availableTypes.ParseValue(dataValue.Type, dataValue.Value);

            if (parsedObject == null)
            {
                throw new Exception($"Cannot parse value \"{dataValue.Value}\" of type: {dataValue.Type}");
            }
            
            property.SetValue(this, parsedObject);
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