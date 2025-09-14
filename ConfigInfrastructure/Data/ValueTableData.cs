using System;
using System.Collections.Generic;

namespace ConfigGenerator.ConfigInfrastructure.Data
{
    [Serializable]
    public class ValueTableData : TableData
    {
        public List<ValueTableDataItem> DataValues = new();
    }
}