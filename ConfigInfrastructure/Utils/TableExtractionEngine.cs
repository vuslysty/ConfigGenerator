using System;
using System.Collections.Generic;
using ConfigGenerator.ConfigInfrastructure.Data;

namespace ConfigGenerator.ConfigInfrastructure.Utils
{
    public static class PrimaryKey
    {
        public const int Id = 1;
        public const int Const = 2;
        public const int Enum = 3;

        private static readonly Dictionary<int, string> _keyToString = new Dictionary<int, string>()
        {
            [Id] = "id",
            [Const] = "const",
            [Enum] = "enum",
        };

        public static bool IsPrimaryKey(string? value)
        {
            return IsPrimaryKey(value, out _);
        }

        public static bool IsPrimaryKey(string? value, out int key)
        {
            key = -1;

            if (value == null)
            {
                return false;
            }

            value = value.Trim();

            foreach (KeyValuePair<int, string> pair in _keyToString)
            {
                if (string.Equals(value, pair.Value, StringComparison.InvariantCultureIgnoreCase))
                {
                    key = pair.Key;
                    return true;
                }
            }

            return false;
        }
    }

    public static class TableExtractionEngine
    {
        private static readonly List<ITableCandidateParser> _candidateParsers = new()
        {
            new ValueTableCandidateParser(),
            new DatabaseTableCandidateParser(),
            new ConstantTableCandidateParser()
        };

        public static bool ExtractTablesFromPage(string pageName, IList<IList<object>> pageData, out List<TableData> tableDataList)
        {
            tableDataList = new();

            List<TableCandidate> possibleTables = TableCandidateLocator.GetPossibleTables(pageName, pageData);

            foreach (TableCandidate possibleTable in possibleTables)
            {
                string tableName = TableNameNormalizationService.ExtractTypeName(possibleTable.Name);
                TableParsingContext context = new TableParsingContext(
                    tableName,
                    possibleTable.PrimaryKey,
                    possibleTable.Row,
                    possibleTable.Col,
                    pageData);

                foreach (ITableCandidateParser candidateParser in _candidateParsers)
                {
                    if (!candidateParser.TryParse(context, out TableData? tableData))
                    {
                        continue;
                    }

                    tableDataList.Add(tableData);
                    break;
                }
            }

            if (!TableMetadataValidationService.ValidateTablesByOverlapping(tableDataList))
            {
                return false;
            }

            if (!TableMetadataValidationService.ValidateTablesByNamePatterns(tableDataList))
            {
                return false;
            }

            if (!TableMetadataValidationService.ValidateTablesByDuplicatesInNames(tableDataList))
            {
                return false;
            }

            return true;
        }

        public static bool ValidateTableTypesAndValues(TableData tableData, AvailableTypes availableTypes)
        {
            return TableDataValidationService.ValidateTableTypesAndValues(tableData, availableTypes);
        }
    }
}
