using System;
using System.Collections.Generic;

namespace ConfigGenerator.ConfigInfrastructure.Data
{
    [Serializable]
    public class ValueTableDataItem
    {
        [NonSerialized]
        public int Row;
        [NonSerialized]
        public int Height;
        public string Id;
        public string Type;
        public ArrayType ArrayType;

        [NonSerialized]
        public List<int> ValuesRows = new List<int>(); 
        
        public List<string> Values = new List<string>();
        public string Comment;
    }
}