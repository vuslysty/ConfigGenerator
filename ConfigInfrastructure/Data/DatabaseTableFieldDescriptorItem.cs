using System;

namespace ConfigGenerator.ConfigInfrastructure.Data
{
    [Serializable]
    public class DatabaseTableFieldDescriptorItem
    {
        [NonSerialized]
        public int Col;
        public string FieldName;
        public string TypeName;
        public string Comment;
    }
}