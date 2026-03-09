namespace ConfigGenerator.ConfigInfrastructure.Utils;

internal static class ColumnIndexFormatter
{
    public static string ToColumnName(int index)
    {
        string column = string.Empty;

        while (index >= 0)
        {
            column = (char)('A' + (index % 26)) + column;
            index = index / 26 - 1;
        }

        return column;
    }
}
