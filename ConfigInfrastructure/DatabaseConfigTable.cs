using System;
using System.Collections.Generic;

namespace ConfigGenerator.ConfigInfrastructure;

public abstract class DatabaseConfigTable<TItem, TId> : IDatabaseConfigTable 
    where TItem : IConfigTableItem<TId>
    where TId : notnull
{
    public List<TItem> Items { get; } = new();
    public int Count => Items.Count;

    public TItem this[TId id] => GetItemWithId(id);

    private readonly Dictionary<TId, TItem> _idToItemMap = new();

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
    
    public void Initialize(TableData tableData, AvailableTypes availableTypes)
    {
        if (tableData is not DatabaseTableData databaseTableData)
        {
            throw new Exception($"TableData must be of type {typeof(DatabaseTableData)}");
        }
        
        Items.Clear();

        Type itemType = typeof(TItem);

        for (var lineIndex = 0; lineIndex < databaseTableData.ValueLines.Count; lineIndex++)
        {
            var lineData = databaseTableData.ValueLines[lineIndex];
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
            
            indexProperty.SetValue(item, lineIndex);
            
            var idProperty = itemType.GetProperty("Id");
            
            if (idProperty == null)
            {
                throw new Exception($"Not found Id property in type {itemType}");
            }
            
            if (!availableTypes.ParseValue(databaseTableData.IdType, lineData.Id, out var idValue))
            {
                throw new Exception($"Cannot parse id value \"{lineData.Id}\" of type: {databaseTableData.IdType}");
            }
            
            idProperty.SetValue(item, idValue);

            if (databaseTableData.FieldDescriptors.Count != lineData.Values.Count)
            {
                throw new Exception("Different number of values and field descriptors");
            }
            
            for (var fieldIndex = 0; fieldIndex < databaseTableData.FieldDescriptors.Count; fieldIndex++)
            {
                var fieldDescriptor = databaseTableData.FieldDescriptors[fieldIndex];
                var property = itemType.GetProperty(fieldDescriptor.FieldName);

                if (property == null)
                {
                    throw new Exception($"Not found property \"{fieldDescriptor.FieldName}\" in type {itemType}");
                }
                
                bool isTableType = typeof(IConfigTableItem<string>).IsAssignableFrom(property.PropertyType);

                if (isTableType)
                {
                    // Properties with IConfigTable types we fill on PostInitialize step
                    continue;
                }

                string strValueData = lineData.Values[fieldIndex];
                
                if (!availableTypes.ParseValue(fieldDescriptor.TypeName, strValueData, out var parsedValue))
                {
                    throw new Exception($"Cannot parse value \"{strValueData}\" of type: {fieldDescriptor.TypeName}");
                }
                
                property.SetValue(item, parsedValue);
            }
            
            Items.Add(item);
        }

        foreach (TItem item in Items)
        {
            _idToItemMap[item.Id] = item;
        }
    }
    
    public virtual void PostInitialize(TableData tableData, AvailableTypes availableTypes)
    {
        if (tableData is not DatabaseTableData databaseTableData)
        {
            throw new Exception($"TableData must be of type {typeof(DatabaseTableData)}");
        }
        
        Type itemType = typeof(TItem);

        for (var fieldIndex = 0; fieldIndex < databaseTableData.FieldDescriptors.Count; fieldIndex++)
        {
            var fieldDescriptor = databaseTableData.FieldDescriptors[fieldIndex];
            var property = itemType.GetProperty(fieldDescriptor.FieldName);
            
            if (property == null)
            {
                throw new Exception($"Not found property \"{fieldDescriptor.FieldName}\" in type {itemType}");
            }
            
            bool isTableType = typeof(IConfigTableItem<string>).IsAssignableFrom(property.PropertyType);

            if (!isTableType)
            {
                continue;
            }

            for (var lineIndex = 0; lineIndex < databaseTableData.ValueLines.Count; lineIndex++)
            {
                var lineData = databaseTableData.ValueLines[lineIndex];
                string valueStr = lineData.Values[fieldIndex];

                if (!availableTypes.ParseValue(fieldDescriptor.TypeName, valueStr, out var parsedValue))
                {
                    throw new Exception($"Cannot parse value \"{valueStr}\" of type: {fieldDescriptor.TypeName}");
                }

                var item = Items[lineIndex];
                property.SetValue(item, parsedValue);
            }
        }
    }
}