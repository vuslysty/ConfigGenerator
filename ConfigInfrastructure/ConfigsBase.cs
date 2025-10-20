using System;
using System.Collections.Generic;
using System.Reflection;
using ConfigGenerator.ConfigInfrastructure.Data;
using ConfigGenerator.ConfigInfrastructure.TypeDesctiptors;

namespace ConfigGenerator.ConfigInfrastructure
{
    internal interface IInitializable
    {
        void Initialize();
    }

    public abstract class ConfigsBase
    {
        private class DatabaseTypeDescriptor : TypeDescriptor, IInitializable
        {
            private readonly AvailableTypes _availableTypes;
            private readonly Type _idType;
            private readonly IDatabaseConfigTable _databaseConfigTable;

            private TypeDescriptor _idTypeDescriptor;
            
            public DatabaseTypeDescriptor(AvailableTypes availableTypes, Type idType, IDatabaseConfigTable databaseConfigTable, string tableName, Type tableItemType) 
                : base(tableName, $"{tableName}.Item", tableItemType)
            {
                _availableTypes = availableTypes;
                _idType = idType;
                _databaseConfigTable = databaseConfigTable;
            }

            public void Initialize() {
                _idTypeDescriptor = _availableTypes.GetTypeDescriptor(_idType);

                if (_idTypeDescriptor == null) {
                    throw new Exception("Could not find available type for id: " + _idType);
                }
            }

            public override bool Parse(string value, out object? result)
            {
                result = null;

                if (string.IsNullOrEmpty(value))
                {
                    return true;
                }
            
                if (_databaseConfigTable.TryGetItemWithTableStringId(value, out var item))
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
                if (tableData is ConstantTableData constantTableData) {
                    availableTypes.Register(new ConstantTableTypeDescriptor(constantTableData));
                }
            }

            foreach (var tableData in tables)
            {
                if (_nameToConfigTableMap.TryGetValue(tableData.Name, out IConfigTable configTable)) {
                    configTable.Initialize(tableData, availableTypes);
                }
            }
        
            foreach (var tableData in tables)
            {
                if (_nameToConfigTableMap.TryGetValue(tableData.Name, out IConfigTable configTable)) {
                    configTable.PostInitialize(tableData, availableTypes);
                }
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
            
                Type tableItemType = genericArguments[0];
                
                if (!requiredItemType.IsAssignableFrom(tableItemType))
                    continue;
            
                Type idType = genericArguments[1];
                var configTableInstance = (IDatabaseConfigTable)fieldInfo.GetValue(this);
                
                DatabaseTypeDescriptor descriptor = new DatabaseTypeDescriptor(availableTypes, idType, 
                    configTableInstance, fieldInfo.FieldType.Name, tableItemType);
            
                availableTypes.Register(descriptor);
            }
            
            availableTypes.Initialize();
        
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