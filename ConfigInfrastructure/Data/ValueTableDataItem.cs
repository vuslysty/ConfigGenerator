using System;

namespace ConfigGenerator.ConfigInfrastructure.Data
{
    [Serializable]
    public class ValueTableDataItem
    {
        [NonSerialized]
        public int Row;
        public string Id;
        public string Type;
        public string Value;
        public string Comment;
    }
}