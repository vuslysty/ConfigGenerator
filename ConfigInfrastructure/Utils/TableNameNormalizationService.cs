using System.Linq;
using CaseConverter;

namespace ConfigGenerator.ConfigInfrastructure.Utils
{
    public static class TableNameNormalizationService
    {
        public static string ExtractTypeName(string value)
        {
            string result = value;

            if (value.StartsWith('$'))
            {
                result = result.TrimStart('$');
            }

            result = result.ToPascalCase();
            return RemoveWhitespaces(result);
        }

        public static string ExtractFieldName(string value)
        {
            string result = value.ToPascalCase();
            return RemoveWhitespaces(result);
        }

        private static string RemoveWhitespaces(string value)
        {
            return new string(value.Where(c => !char.IsWhiteSpace(c)).ToArray());
        }
    }
}
