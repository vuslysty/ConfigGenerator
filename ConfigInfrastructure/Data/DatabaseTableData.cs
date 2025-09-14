using System;
using System.Collections.Generic;

namespace ConfigGenerator.ConfigInfrastructure.Data
{
    [Serializable]
    public class DatabaseTableData : TableData
    {
        public string IdType;
        public List<DatabaseTableFieldDescriptorItem> FieldDescriptors = new();
        public List<DatabaseTableValuesLineData?> ValueLines = new();
    }
}