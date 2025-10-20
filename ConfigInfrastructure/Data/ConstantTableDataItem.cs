using System;

namespace ConfigGenerator.ConfigInfrastructure.Data
{
    [Serializable]
    public class ConstantTableDataItem
    {
        [NonSerialized]
        public int Row;

        public string Name;
    
        public int Value;
    
        [NonSerialized]
        public string StringValue;
    
        public string Comment;
    }
}