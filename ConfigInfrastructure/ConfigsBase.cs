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
            private readonly string _idTypeName;
            private readonly IDatabaseConfigTable _databaseConfigTable;

            private TypeDescriptor _idTypeDescriptor;
    
            // public DatabaseTypeDescriptor(TypeDescriptor idTypeDescriptor, IDatabaseConfigTable databaseConfigTable, string tableName) 
            //     : base(tableName, $"{tableName}.Item", databaseConfigTable.Type)
            // {
            //     _idTypeDescriptor = idTypeDescriptor;
            //     _databaseConfigTable = databaseConfigTable;
            // }
            
            public DatabaseTypeDescriptor(AvailableTypes availableTypes, string idTypeName, IDatabaseConfigTable databaseConfigTable, string tableName, Type tableItemType) 
                : base(tableName, $"{tableName}.Item", tableItemType)
            {
                _availableTypes = availableTypes;
                _idTypeName = idTypeName;
                _databaseConfigTable = databaseConfigTable;
            }

            public void Initialize() {
                _idTypeDescriptor = _availableTypes.GetTypeDescriptor(_idTypeName);

                if (_idTypeDescriptor == null) {
                    throw new Exception("Could not find available type for id: " + _idTypeName);
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
            
                Type tableItemType = genericArguments[0];
                
                if (!requiredItemType.IsAssignableFrom(tableItemType))
                    continue;
            
                Type idType = genericArguments[1];
                var configTableInstance = (IDatabaseConfigTable)fieldInfo.GetValue(this);

                string idTypeName = requiredItemType.IsAssignableFrom(idType) 
                    ? idType.ReflectedType.Name
                    : idType.Name;
                
                DatabaseTypeDescriptor descriptor = new DatabaseTypeDescriptor(availableTypes, idTypeName, 
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