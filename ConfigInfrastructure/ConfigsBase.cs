using System;
using System.Collections.Generic;
using System.Reflection;

namespace ConfigGenerator.ConfigInfrastructure;

public class ConfigsBase
{
    private class DatabaseTypeDescriptor : TypeDescriptor
    {
        private readonly IDatabaseConfigTable _databaseConfigTable;
    
        public DatabaseTypeDescriptor(IDatabaseConfigTable databaseConfigTable, string tableName) : base(tableName)
        {
            _databaseConfigTable = databaseConfigTable;
        }

        public override object? Parse(string value)
        {
            if (_databaseConfigTable.TryGetItemWithId(value, out var item))
            {
                return item;
            }

            return null;
        }
    }
    
    private bool _initialized;
    private Dictionary<string, IConfigTable> _nameToConfigTableMap;

    public void Initialize(string jsonData)
    {
        var tables = TableDataSerializer.Deserialize(jsonData);
        Initialize(tables);
    }
    
    public void Initialize(List<TableData> tables)
    {
        FillNameToConfigTableMap();
        AvailableTypes availableTypes = GetAvailableTypes();

        foreach (var tableData in tables)
        {
            IConfigTable configTable = _nameToConfigTableMap[tableData.Name];
            configTable.Initialize(tableData, availableTypes);
        }
        
        foreach (var tableData in tables)
        {
            IConfigTable configTable = _nameToConfigTableMap[tableData.Name];
            configTable.PostInitialize(tableData, availableTypes);
        }
        
        _initialized = true;
    }

    protected T GetConfig<T>(T config) where T : IConfigTable
    {
        if (!_initialized)
        {
            Console.WriteLine($"{GetType().Name} not initialized");
            return default!;
        }
        return config;
    }

    private AvailableTypes GetAvailableTypes()
    {
        AvailableTypes availableTypes = new AvailableTypes();
        availableTypes.RegisterDefaultTypes();
        
        Type targetBaseType = typeof(DatabaseConfigTable<,>);
        Type requiredItemType = typeof(IConfigTableItem<string>);
        Type requiredIdType = typeof(string);
        
        Type currentType = GetType();
        
        var fields = currentType.GetFields(BindingFlags.Instance | BindingFlags.NonPublic);

        foreach (var fieldInfo in fields)
        {
            var baseType = fieldInfo.FieldType.BaseType;
            
            if (baseType == null)
                continue;
            
            if (!baseType.IsGenericType)
                continue;
            
            if (baseType.GetGenericTypeDefinition() != targetBaseType)
                continue;

            var genericArguments = baseType.GetGenericArguments();
            
            if (genericArguments.Length != 2)
                continue;
            
            if (!requiredItemType.IsAssignableFrom(genericArguments[0]))
                continue;
            
            if (genericArguments[1] != requiredIdType)
                continue;
            
            var configTableInstance = (IDatabaseConfigTable)fieldInfo.GetValue(this);
            var descriptor = new DatabaseTypeDescriptor(configTableInstance, fieldInfo.FieldType.Name);
            
            availableTypes.Register(descriptor);
        }
        
        return availableTypes;
    }

    private void FillNameToConfigTableMap()
    {
        _nameToConfigTableMap = new Dictionary<string, IConfigTable>();

        FieldInfo[] fields = GetType().GetFields(BindingFlags.NonPublic | BindingFlags.Instance);
        
        foreach (FieldInfo field in fields)
        {
            if (!typeof(IConfigTable).IsAssignableFrom(field.FieldType))
                continue;
            
            var fieldValue = field.GetValue(this);

            if (fieldValue == null)
            {
                fieldValue = Activator.CreateInstance(field.FieldType);
                field.SetValue(this, fieldValue);
            }
                
            _nameToConfigTableMap[field.FieldType.Name] = (IConfigTable)fieldValue!;
        }
    }
}