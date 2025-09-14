using System;
using System.Collections.Generic;
using System.Reflection;
using ConfigGenerator.ConfigInfrastructure.Data;
using ConfigGenerator.ConfigInfrastructure.TypeDesctiptors;

namespace ConfigGenerator.ConfigInfrastructure
{
    public abstract class ConfigsBase
    {
        private class DatabaseTypeDescriptor : TypeDescriptor
        {
            private readonly TypeDescriptor _idTypeDescriptor;
            private readonly IDatabaseConfigTable _databaseConfigTable;
    
            public DatabaseTypeDescriptor(TypeDescriptor idTypeDescriptor, IDatabaseConfigTable databaseConfigTable, string tableName) 
                : base(tableName, $"{tableName}.Item", databaseConfigTable.Type)
            {
                _idTypeDescriptor = idTypeDescriptor;
                _databaseConfigTable = databaseConfigTable;
            }

            public override bool Parse(string value, out object? result)
            {
                result = null;

                if (string.IsNullOrEmpty(value))
                {
                    return true;
                }

                if (!_idTypeDescriptor.Parse(value, out object? parsedValue))
                {
                    return false;
                }
            
                if (_databaseConfigTable.TryGetItemWithId(parsedValue!, out var item))
                {
                    result = item;
                    return true;
                }

                return false;
            }
        }
    
        protected bool _initialized;
        private Dictionary<string, IConfigTable> _nameToConfigTableMap;

        protected void Initialize(string jsonData, ITableDataSerializer tableDataSerializer)
        {
            var tables = tableDataSerializer.Deserialize(jsonData);
            Initialize(tables);
        }
    
        protected void Initialize(List<TableData> tables)
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

        private AvailableTypes GetAvailableTypes()
        {
            AvailableTypes availableTypes = new AvailableTypes();
            availableTypes.RegisterDefaultTypes();
        
            Type targetBaseType = typeof(DatabaseConfigTable<,>);
            Type requiredItemType = typeof(IConfigTableItem);
        
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
            
                Type idType = genericArguments[1];
                var configTableInstance = (IDatabaseConfigTable)fieldInfo.GetValue(this);

                DatabaseTypeDescriptor descriptor = null;
            
                if (idType == typeof(int))
                {
                    descriptor = new DatabaseTypeDescriptor(AvailableTypes.Int, configTableInstance, fieldInfo.FieldType.Name);
                }
                else if (idType == typeof(string))
                {
                    descriptor = new DatabaseTypeDescriptor(AvailableTypes.String, configTableInstance, fieldInfo.FieldType.Name);
                }
                else
                {
                    throw new Exception($"Unknown ID type: {idType}");
                }
            
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
}