using System;
using System.Collections.Generic;

namespace ConfigGenerator.ConfigInfrastructure.Data
{
    [Serializable]
    public class DatabaseTableValuesLineData
    {
        [NonSerialized]
        public int Row;
        public string Id;
        public List<string> Values = new();
    }
}