namespace ConfigGenerator.ConfigInfrastructure
{
    public interface IConfigTableItem
    {
    
    }

    public class ConfigTableItem<T> : IConfigTableItem
    {
        public int Index { get; protected set; }
        public T Id { get; protected set; }
    }
}