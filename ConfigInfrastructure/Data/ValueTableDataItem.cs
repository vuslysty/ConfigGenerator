using System;

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
        public string Value;
        public string Comment;
    }
}