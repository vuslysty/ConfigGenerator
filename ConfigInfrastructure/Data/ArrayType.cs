namespace ConfigGenerator.ConfigInfrastructure.Data
{
    public enum ArrayType
    {
        None,
        OneCell,
        Multicell,
    }

    public static class ArrayTypeExtensions
    {
        public static bool IsArray(this ArrayType type) {
            return type == ArrayType.OneCell || type == ArrayType.Multicell;
        }
    }
}