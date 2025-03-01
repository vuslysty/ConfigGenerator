namespace ConfigGenerator.ConfigInfrastructure;

public interface IConfigTableItem<T>
{
    int Index { get; }
    T Id { get; }
}