using System;
using System.Collections.Generic;

namespace ConfigGenerator.ConfigInfrastructure.Data
{
    [Serializable]
    public class ConstantTableData : TableData
    {
        public List<ConstantTableDataItem> Items = new();
    }
}