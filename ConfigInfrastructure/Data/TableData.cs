using System;

namespace ConfigGenerator.ConfigInfrastructure.Data
{
    [Serializable]
    public class TableData
    {
        public string Name;
        
        [NonSerialized]
        public int StartRow;
        [NonSerialized]
        public int StartCol;
        
        [NonSerialized]
        public int EndRow;
        [NonSerialized]
        public int EndCol;
    }
}