using System;
using System.Collections.Generic;

namespace ConfigGenerator.ConfigInfrastructure.Data
{
    [Serializable]
    public class DatabaseTableData : TableData
    {
        public FieldNode RootFieldNode;
        public List<DataObject> DataObjects;
        
        public string IdType => RootFieldNode.Children[0].BaseType;
    }
    
    [Serializable]
    public class FieldNode
    {
        public string Name;
        public string BaseType;
        
        [NonSerialized]
        public string? CustomType;
        
        public ArrayType ArrayType;
        
        public string? Comment;
        public int ColumnIndex = -1;

        public List<FieldNode> Children = new();
        public string? ArrayDelimiter;
    }

    [Serializable]
    public class DataField
    {
        public string Name;
        public int RowIndex;
        public int ColumnIndex;
        public List<string> Values = new();
        
        
        public List<int> ValuesRows;
        
        public int Height;
    }

    [Serializable]
    public class DataObject
    {
        public int RowIndex;
        public int ColumnIndex;
        public int Height;
        public List<DataField> Fields = new();   // прості поля
        public List<DataArray> Arrays = new();   // масиви об'єктів
    }

    [Serializable]
    public class DataArray
    {
        public string Name;
        public int RowIndex;
        public int ColumnIndex;
        public int Height;
        public List<DataObject> Items = new();
    }
}