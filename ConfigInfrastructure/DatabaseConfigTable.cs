using System;
using System.Collections;
using System.Collections.Generic;
using ConfigGenerator.ConfigInfrastructure.Data;
using ConfigGenerator.ConfigInfrastructure.TypeDesctiptors;

namespace ConfigGenerator.ConfigInfrastructure
{
    public abstract class DatabaseConfigTable<TItem, TId> : IDatabaseConfigTable 
        where TItem : ConfigTableItem<TId>
        where TId : notnull
    {
        public List<TItem> Items { get; } = new();
        public int Count => Items.Count;

        public TItem this[TId id] => GetItemWithId(id);

        private readonly Dictionary<TId, TItem> _idToItemMap = new();
        private readonly Dictionary<string, TItem> _stringIdToItemMap = new();

        public Type Type { get; }

        protected DatabaseConfigTable()
        {
            Type = typeof(TItem);
        }
    
        public TItem GetItemWithId(TId id)
        {
            if (TryGetItemWithId(id, out TItem item))
            {
                return item;
            }

            return default!;
        }
    
        public bool TryGetItemWithId(TId id, out TItem item)
        {
            return _idToItemMap.TryGetValue(id, out item);
        }
    
        public bool TryGetItemWithId(object key, out object value)
        {
            value = null;
        
            if (key is not TId id)
            {
                return false;
            }

            if (TryGetItemWithId(id, out TItem result))
            {
                value = result;
                return true;
            }

            return false;
        }

        public bool TryGetItemWithTableStringId(string id, out object value)
        {
            value = null;
            
            if (!_stringIdToItemMap.TryGetValue(id, out var item)) {
                return false;
            }

            value = item;
            
            return true;
        }

        private void InitializeItemWithLinksToOtherTables(object item, DataObject dataObject,
            AvailableTypes availableTypes,
            string? itemTypeName = null)
        {
            if (itemTypeName == null) {
                itemTypeName = "Item";
            }
    
            Dictionary<string, FieldNode> fieldNodeByName = _fullClassTypeNameToFieldTypes[itemTypeName];
            
            Type itemType = item.GetType();

            foreach (DataField field in dataObject.Fields)
            {
                var property = itemType.GetProperty(field.Name);
                
                if (property == null)
                {
                    throw new Exception($"Not found property \"{field.Name}\" in type {itemTypeName}");
                }
                
                FieldNode fieldNode = fieldNodeByName[field.Name];
                
                Type tableType = typeof(IConfigTableItem);
                bool isTableType = tableType.IsAssignableFrom(property.PropertyType)
                                   || tableType.MakeArrayType().IsAssignableFrom(property.PropertyType);
                
                if (!isTableType) {
                    continue;
                }
                
                object? parsedValue;
                
                if (fieldNode.ArrayType.IsArray())
                {
                    if (!availableTypes.ParseAsArray(fieldNode.BaseType, field.Values, out parsedValue))
                    {
                        throw new Exception($"Cannot parse array values \"{field.Values}\" " +
                                            $"of type: {fieldNode.BaseType} " +
                                            $"in type: {itemTypeName}");
                    }
                }
                else
                {
                    string strValueData = field.Values.Count > 0 ? field.Values[0] : string.Empty;
                
                    if (!availableTypes.ParseValue(fieldNode.BaseType, strValueData, out parsedValue))
                    {
                        throw new Exception($"Cannot parse value \"{strValueData}\" " +
                                            $"of type: {fieldNode.BaseType} " +
                                            $"in type: {itemTypeName}");
                    }
                }
                
                property.SetValue(item, parsedValue);
            }
            
            foreach (DataArray array in dataObject.Arrays)
            {
                var property = itemType.GetProperty(array.Name);
                
                if (property == null)
                {
                    throw new Exception($"Not found property \"{array.Name}\" in type {itemTypeName}");
                }
                
                IList list = (IList)property.GetValue(item);
                
                if (array.Items.Count != list.Count)
                {
                    // TODO add more informational message
                    throw new Exception("Incorrect number of items");
                }
                
                FieldNode fieldNode = fieldNodeByName[array.Name];
                string arrayItemTypeName = $"{itemTypeName}.{fieldNode.BaseType}";

                for (var index = 0; index < array.Items.Count; index++)
                {
                    var arrayItemData = array.Items[index];
                    object arrayItem = list[index];
                    
                    InitializeItemWithLinksToOtherTables(arrayItem, arrayItemData, availableTypes, arrayItemTypeName);
                }
            }
        }

        private void InitializeItemWithDataObject(object item, DataObject dataObject, AvailableTypes availableTypes, 
            string? itemTypeName = null)
        {
            if (itemTypeName == null) {
                itemTypeName = "Item";
            }
    
            Dictionary<string, FieldNode> fieldNodeByName = _fullClassTypeNameToFieldTypes[itemTypeName];
            
            Type itemType = item.GetType();
            
            foreach (DataField field in dataObject.Fields)
            {
                var property = itemType.GetProperty(field.Name);
                
                if (property == null)
                {
                    throw new Exception($"Not found property \"{field.Name}\" in type {itemTypeName}");
                }
                
                FieldNode fieldNode = fieldNodeByName[field.Name];
                
                Type tableType = typeof(IConfigTableItem);
                bool isTableType = tableType.IsAssignableFrom(property.PropertyType)
                                   || tableType.MakeArrayType().IsAssignableFrom(property.PropertyType);
                
                if (isTableType)
                {
                    // Properties with IConfigTable types we fill on PostInitialize step
                    continue;
                }
                
                object? parsedValue;
                
                if (fieldNode.ArrayType.IsArray())
                {
                    if (!availableTypes.ParseAsArray(fieldNode.BaseType, field.Values, out parsedValue))
                    {
                        throw new Exception($"Cannot parse array values \"{field.Values}\" " +
                                            $"of type: {fieldNode.BaseType} " +
                                            $"in type: {itemTypeName}");
                    }
                }
                else
                {
                    string strValueData = field.Values.Count > 0 ? field.Values[0] : string.Empty;
                
                    if (!availableTypes.ParseValue(fieldNode.BaseType, strValueData, out parsedValue))
                    {
                        throw new Exception($"Cannot parse value \"{strValueData}\" " +
                                            $"of type: {fieldNode.BaseType} " +
                                            $"in type: {itemTypeName}");
                    }
                }
                
                property.SetValue(item, parsedValue);
            }
            
            foreach (DataArray array in dataObject.Arrays)
            {
                var property = itemType.GetProperty(array.Name);
                
                if (property == null)
                {
                    throw new Exception($"Not found property \"{array.Name}\" in type {itemTypeName}");
                }
                
                Type elementType = property.PropertyType.GetGenericArguments()[0];
                
                // Створюємо список потрібного типу
                Type listType = typeof(List<>).MakeGenericType(elementType);
                IList list = (IList)Activator.CreateInstance(listType);
                
                FieldNode fieldNode = fieldNodeByName[array.Name];
                string arrayItemTypeName = $"{itemTypeName}.{fieldNode.BaseType}";
                
                foreach (DataObject arrayItemData in array.Items)
                {
                    object arrayItem = Activator.CreateInstance(elementType);
                    InitializeItemWithDataObject(arrayItem, arrayItemData, availableTypes, arrayItemTypeName);
                    list.Add(arrayItem);
                }
                
                property.SetValue(item, list);
            }
        }
        
        private Dictionary<string, Dictionary<string, FieldNode>> _fullClassTypeNameToFieldTypes =
            new Dictionary<string, Dictionary<string, FieldNode>>();

        private void InitClassNameToFieldTypesCache(string fullClassPath, FieldNode fieldNode)
        {
            Dictionary<string, FieldNode> fieldNameToFieldNode = new();
            _fullClassTypeNameToFieldTypes[fullClassPath] = fieldNameToFieldNode;
            
            foreach (FieldNode child in fieldNode.Children)
            {
                fieldNameToFieldNode[child.Name] = child;
                
                if (child.Children.Count > 0)
                {
                    InitClassNameToFieldTypesCache($"{fullClassPath}.{child.BaseType}", child);
                }
            }
        }
        
        public void Initialize(TableData tableData, AvailableTypes availableTypes)
        {
            if (tableData is not DatabaseTableData databaseTableData)
            {
                throw new Exception($"TableData must be of type {typeof(DatabaseTableData)}");
            }
            
            _fullClassTypeNameToFieldTypes.Clear();
            InitClassNameToFieldTypesCache("Item", databaseTableData.RootFieldNode);
            
            Items.Clear();
            
            Type itemType = typeof(TItem);
            
            for (var index = 0; index < databaseTableData.DataObjects.Count; index++)
            {
                var dataObject = databaseTableData.DataObjects[index];
                var item = (TItem)Activator.CreateInstance(itemType);
            
                if (item == null)
                {
                    throw new Exception($"Cannot create instance of type {itemType}");
                }
                
                var indexProperty = itemType.GetProperty("Index");
            
                if (indexProperty == null)
                {
                    throw new Exception($"Not found Index property in type {itemType}");
                }
            
                indexProperty.SetValue(item, index);
                
                InitializeItemWithDataObject(item, dataObject, availableTypes);
                Items.Add(item);

                string stringId = dataObject.Fields[0].Values[0];
                _stringIdToItemMap[stringId] = item;
            }
        }
    
        public virtual void PostInitialize(TableData tableData, AvailableTypes availableTypes)
        {
            if (tableData is not DatabaseTableData databaseTableData)
            {
                throw new Exception($"TableData must be of type {typeof(DatabaseTableData)}");
            }
            
            Type idType = typeof(TId);
            
            bool isIdAsItemFromOtherTable = typeof(IConfigTableItem).IsAssignableFrom(idType);
            
            string idTypeName = isIdAsItemFromOtherTable 
                ? idType.ReflectedType.Name
                : idType.Name;
                
            TypeDescriptor idTypeDescriptor = availableTypes.GetTypeDescriptor(idTypeName);

            if (isIdAsItemFromOtherTable) {
                foreach (KeyValuePair<string, TItem> pair in _stringIdToItemMap)
                {
                    if (!idTypeDescriptor.Parse(pair.Key, out var parsedIdItem)) {
                        // TODO informational message
                        throw new Exception("Cannot parse string id");
                    }

                    TId idItem = (TId)parsedIdItem;
                    _idToItemMap[idItem] = pair.Value;
                }
            } else {
                foreach (TItem item in Items)
                {
                    _idToItemMap[item.Id] = item;
                }
            }

            if (databaseTableData.DataObjects.Count != Items.Count)
            {
                // TODO add more informational message
                throw new Exception("Incorrect number of items");
            }
            
            for (var index = 0; index < databaseTableData.DataObjects.Count; index++)
            {
                var dataObject = databaseTableData.DataObjects[index];
                var item = Items[index];
                
                InitializeItemWithLinksToOtherTables(item, dataObject, availableTypes);
            }
        }
    }
}