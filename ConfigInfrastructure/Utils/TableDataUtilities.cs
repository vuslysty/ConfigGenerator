using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Text.RegularExpressions;
using CaseConverter;
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

        public static bool IsPrimaryKey(string? value) {
            return IsPrimaryKey(value, out _);
        }
        
        public static bool IsPrimaryKey(string? value, out int key)
        {
            key = -1;
                
            if (value == null) {
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
    
    public static class TableDataUtilities
    {
        private const string TableStartPattern = @"^#([A-Za-z][A-Za-z0-9 ]*)$";

        public static bool ExtractTablesFromPage(string pageName, IList<IList<object>> pageData, out List<TableData> tableDataList)
        {
            tableDataList = new();
            
            List<PossibleTableData> possibleTables = GetPossibleTables(pageName, pageData);
            
            foreach (PossibleTableData possibleTableData in possibleTables)
            {
                int startRow = possibleTableData.Row;
                int startCol = possibleTableData.Col;
                
                string tableName = ExtractTypeName(possibleTableData.Name);

                switch (possibleTableData.PrimaryKey)
                {
                    case PrimaryKey.Id:
                    {
                        string? possibleTypeStr = GetCellData(pageData, startRow, startCol + 1);
                        possibleTypeStr = possibleTypeStr?.Trim();
                        
                        string? possibleValueStr = GetCellData(pageData, startRow, startCol + 2);
                        possibleValueStr = possibleValueStr?.Trim();
                        
                        if (string.Equals(possibleTypeStr, "type", StringComparison.InvariantCultureIgnoreCase) &&
                            string.Equals(possibleValueStr, "value", StringComparison.InvariantCultureIgnoreCase))
                        {
                            ValueTableData valueTableData = GetValueTableData(startRow, startCol, tableName, pageData);
                            tableDataList.Add(valueTableData);
                        }
                        else
                        {
                            DatabaseTableData databaseTableData = GetDatabaseTableData(startRow, startCol, tableName, pageData);
                            tableDataList.Add(databaseTableData);
                        }
                        break;
                    }
                    case PrimaryKey.Const:
                    {
                        string? possibleValueStr = GetCellData(pageData, startRow, startCol + 1);
                        possibleValueStr = possibleValueStr?.Trim();

                        if (string.Equals(possibleValueStr, "value", StringComparison.InvariantCultureIgnoreCase))
                        {
                            ConstantTableData constantTableData = GetConstantTableData(startRow, startCol, tableName, pageData);
                            tableDataList.Add(constantTableData);
                        }
                        
                        break;
                    }
                }
            }

            if (!ValidateTablesByOverlapping(tableDataList))
            {
                return false;
            }
            
            if (!ValidateTablesByNamePatterns(tableDataList))
            {
                return false;
            }
            
            if (!ValidateTablesByDuplicatesInNames(tableDataList))
            {
                return false;
            }
            
            return true;
        }

        public static bool ValidateTableTypesAndValues(TableData tableData, AvailableTypes availableTypes)
        {
            bool isValid = true;
            
            switch (tableData)
            {
                case ValueTableData valueTableData:
                {
                    foreach (ValueTableDataItem dataValue in valueTableData.Items)
                    {
                        var typeDescriptor = availableTypes.GetTypeDescriptor(dataValue.Type);

                        if (typeDescriptor == null)
                        {
                            isValid = false;
                            Console.WriteLine($"Error: used invalid data type \"{dataValue.Type}\". " +
                                              $"Table: {valueTableData.Name}, " +
                                              $"Row: {dataValue.Row + 1}, " +
                                              $"Col: {IndexToColumn(valueTableData.StartCol + 1)}");
                            
                            continue;
                        }

                        if (dataValue.ArrayType.IsArray())
                        {
                            for (var i = 0; i < dataValue.Values.Count; i++)
                            {
                                string value = dataValue.Values[i];
                                int valueRow = dataValue.Row;

                                switch (dataValue.ArrayType)
                                {
                                    case ArrayType.OneCell:
                                        valueRow = dataValue.ValuesRows.First();
                                        break;
                                    
                                    case ArrayType.Multicell:
                                        valueRow = dataValue.ValuesRows[i];
                                        break;
                                }
                                
                                if (!typeDescriptor.Parse(value, out var parsedValue))
                                {
                                    isValid = false;
                                    Console.WriteLine($"Error: used invalid data value \"{value}\", " +
                                                      $"for type: \"{dataValue.Type}\". " +
                                                      $"Table: {valueTableData.Name}, " +
                                                      $"Row: {valueRow + 1}, " +
                                                      $"Col: {IndexToColumn(valueTableData.StartCol + 2)}");
                                }
                            }
                        } 
                        else
                        {
                            string value = dataValue.Values.Count > 0 ? dataValue.Values.First() : string.Empty;
                            int valueRow = dataValue.ValuesRows.Count > 0 ? dataValue.ValuesRows.First() : dataValue.Row;
                            
                            if (!typeDescriptor.Parse(value, out var parsedValue))
                            {
                                isValid = false;
                                Console.WriteLine($"Error: used invalid data value \"{value}\", " +
                                                  $"for type: \"{dataValue.Type}\". " +
                                                  $"Table: {valueTableData.Name}, " +
                                                  $"Row: {valueRow + 1}, " +
                                                  $"Col: {IndexToColumn(valueTableData.StartCol + 2)}");
                            }
                        }
                    }
                    
                    break;
                }
                
                case DatabaseTableData databaseTableData:
                {
                    foreach (DataObject dataObject in databaseTableData.DataObjects)
                    {
                        if (!IsValidDataObject(dataObject, databaseTableData.RootFieldNode, databaseTableData,
                                availableTypes))
                        {
                            isValid = false;
                        }
                    }
                    
                    break;
                }

                case ConstantTableData constantTableData:
                {
                    // We've already validated constants
                    break;
                }
                
                default:
                {
                    Console.WriteLine("Error: tried to validate an unsupported table type");
                    isValid = false;
                    break;
                }
            }

            return isValid;
        }
        
        private static bool IsValidDataObject(DataObject dataObject, FieldNode baseFieldNode, DatabaseTableData databaseTableData, AvailableTypes availableTypes)
        {
            bool isValid = true;
        
            foreach (DataField dataField in dataObject.Fields)
            {
                var currentFieldNode = baseFieldNode.Children.Find(childFieldNode => childFieldNode.Name == dataField.Name);
                var typeDescriptor = availableTypes.GetTypeDescriptor(currentFieldNode.BaseType);
                
                if (typeDescriptor == null)
                {
                    isValid = false;
                    Console.WriteLine($"Error: used invalid data type \"{currentFieldNode.BaseType}\". " +
                                      $"Table: {databaseTableData.Name}, " +
                                      $"Row: {dataField.RowIndex + 1}, " +
                                      $"Col: {IndexToColumn(dataField.ColumnIndex)}");
                    
                    continue;
                }
                
                if (currentFieldNode.ArrayType.IsArray())
                {
                    for (int i = 0; i < dataField.Values.Count; i++)
                    {
                        string value = dataField.Values[i];
                        int valueRow = dataField.RowIndex;
                        
                        switch (currentFieldNode.ArrayType)
                        {
                            case ArrayType.OneCell:
                                valueRow = dataField.ValuesRows.First();
                                break;
                                    
                            case ArrayType.Multicell:
                                valueRow = dataField.ValuesRows[i];
                                break;
                        }
                        
                        if (!typeDescriptor.Parse(value, out var parsedValue))
                        {
                            isValid = false;
                            Console.WriteLine($"Error: used invalid data value \"{value}\", " +
                                              $"for field: \"{currentFieldNode.BaseType} {currentFieldNode.Name}\". " +
                                              $"Table: {databaseTableData.Name}, " +
                                              $"Row: {valueRow + 1}, " +
                                              $"Col: {IndexToColumn(currentFieldNode.ColumnIndex)}");
                        }
                    }
                }
                else
                {
                    string value = dataField.Values.Count > 0 ? dataField.Values.First() : string.Empty;
                    int valueRow = dataField.ValuesRows.Count > 0 ? dataField.ValuesRows.First() : dataField.RowIndex;
                    
                    if (!typeDescriptor.Parse(value, out var parsedValue))
                    {
                        isValid = false;
                        Console.WriteLine($"Error: used invalid data value \"{value}\", " +
                                          $"for field: \"{currentFieldNode.BaseType} {currentFieldNode.Name}\". " +
                                          $"Table: {databaseTableData.Name}, " +
                                          $"Row: {valueRow + 1}, " +
                                          $"Col: {IndexToColumn(currentFieldNode.ColumnIndex)}");
                    }
                }
            }
        
            foreach (DataArray array in dataObject.Arrays)
            {
                var currentFieldNode = baseFieldNode.Children.Find(childFieldNode => childFieldNode.Name == array.Name);
                
                foreach (DataObject dataObjectItem in array.Items) {
                    if (!IsValidDataObject(dataObjectItem, currentFieldNode, databaseTableData, availableTypes)) {
                        isValid = false;
                    }
                }
            }
            
            return isValid;
        }

        private struct PossibleTableData
        {
            public string Name;
            public int PrimaryKey;
            public int Row;
            public int Col;

            public PossibleTableData(string name, int primaryKey, int row, int col)
            {
                Name = name;
                PrimaryKey = primaryKey;
                Row = row;
                Col = col;
            }
        }
        
        private static List<PossibleTableData> GetPossibleTables(string pageName, IList<IList<object>> pageData)
        {
            List<PossibleTableData> possibleTables = new();

            for (int row = 0; row < pageData.Count; row++)
            {
                int columnCount = pageData[row].Count;

                for (int col = 0; col < columnCount; col++)
                {
                    string cellData = (string)pageData[row][col];

                    if (row == 0 && col == 0 && (string.IsNullOrWhiteSpace(cellData) || PrimaryKey.IsPrimaryKey(cellData, out int primaryKey)))
                    {
                        if (PrimaryKey.IsPrimaryKey(cellData, out primaryKey))
                        {
                            possibleTables.Add(new PossibleTableData(pageName, primaryKey, row, col));
                        }
                        else if (string.IsNullOrEmpty(cellData))
                        {
                            if (TryGetCellData(pageData, row + 1, col, out string nextCellData))
                            {
                                if (PrimaryKey.IsPrimaryKey(nextCellData, out primaryKey))
                                {
                                    possibleTables.Add(new PossibleTableData(pageName, primaryKey, row + 1, col));
                                }
                            }
                        }
                    }
                    else if (cellData.StartsWith('#') && Regex.IsMatch(cellData, TableStartPattern))
                    {
                        string tableName = cellData.Substring(1).Trim();
                        
                        if (TryGetCellData(pageData, row + 1, col, out string nextCellData))
                        {
                            if (PrimaryKey.IsPrimaryKey(nextCellData, out primaryKey))
                            {
                                possibleTables.Add(new PossibleTableData(tableName, primaryKey, row + 1, col));
                            }
                        }
                    }
                }
            }

            return possibleTables;
        }

        private static bool TryGetCellData(IList<IList<object>> pageData, int row, int col, out string cellData)
        {
            cellData = null;

            try
            {
                cellData = (string)pageData[row][col];
                cellData = cellData.Trim();
            }
            catch
            {
                return false;
            }
            
            return true;
        }

        private static string? GetCellData(IList<IList<object>> pageData, int row, int col)
        {
            if (TryGetCellData(pageData, row, col, out string cellData))
            {
                return cellData;
            }
            
            return null;
        }

        private static bool TryGetValueTableDataItem(int startRow, int startCol, IList<IList<object>> pageData, out ValueTableDataItem item)
        {
            item = new ValueTableDataItem()
            {
                Row = startRow,
            };
            
            int idCol = startCol;
            int typeCol = startCol + 1;
            int valueCol = startCol + 2;
            int commentCol = startCol + 3;

            int checkRow = startRow;

            string? idData = GetCellData(pageData, checkRow, idCol);

            if (string.IsNullOrWhiteSpace(idData) || idData.Equals("END")) {
                return false;
            }

            item.Id = idData;
            
            item.Type = GetCellData(pageData, checkRow, typeCol);
            
            string valueData = GetCellData(pageData, checkRow, valueCol);
            List<(int row, string value)> values = new ();
            
            if (!string.IsNullOrWhiteSpace(valueData)) {
                values.Add((checkRow, valueData));
            }
            
            string commentData = GetCellData(pageData, checkRow, commentCol);

            item.Comment = string.IsNullOrWhiteSpace(commentData) ? string.Empty : commentData;
            
            while (true)
            {
                checkRow++;

                if (checkRow >= pageData.Count) {
                    break;
                }
                
                idData = GetCellData(pageData, checkRow, idCol);

                if (!string.IsNullOrWhiteSpace(idData)) {
                    break;
                }

                if (string.IsNullOrWhiteSpace(item.Type)) {
                    item.Type = GetCellData(pageData, checkRow, typeCol);
                }
                
                valueData = GetCellData(pageData, checkRow, valueCol);

                if (!string.IsNullOrWhiteSpace(valueData)) {
                    values.Add((checkRow, valueData));
                }
            
                commentData = GetCellData(pageData, checkRow, commentCol);

                if (!string.IsNullOrWhiteSpace(commentData)) {
                    if (item.Comment == String.Empty) {
                        item.Comment = commentData;
                    } else {
                        item.Comment += '\n';
                        item.Comment += commentData;
                    }
                }
            }

            item.Height = checkRow - startRow;

            if (string.IsNullOrWhiteSpace(item.Type)) {
                item.Type = AvailableTypes.String.TypeName;
            }

            bool isArray = IsArrayType(item.Type, out string delimiter, out string cleanTypeName);
            
            if (isArray) {
                item.Type = cleanTypeName;

                if (delimiter == null) {
                    item.ArrayType = ArrayType.Multicell;
                    
                    foreach ((int row, string value) valueTuple in values) {
                        item.Values.Add(valueTuple.value);
                        item.ValuesRows.Add(valueTuple.row);
                    }
                } else {
                    item.ArrayType = ArrayType.OneCell;
                    
                    if (values.Count > 0) {
                        (int row, string value) valueTuple = values[0];
                        string[] tokens = Tokenize(valueTuple.value, delimiter);
                        item.Values.AddRange(tokens);
                        item.ValuesRows.Add(valueTuple.row);
                    }
                }
            } else {
                item.ArrayType = ArrayType.None;
                
                if (values.Count > 0) {
                    (int row, string value) valueTuple = values[0];
                    item.Values.Add(valueTuple.value);
                    item.ValuesRows.Add(valueTuple.row);
                }
            }

            item.Type = ExtractTypeName(item.Type);
            
            return true;
        }
        
        private static readonly Regex ArrayTypeRegex = new Regex(
            @"^(.+?)\[(.*)\]$", 
            RegexOptions.Compiled
        );
        
        public static bool IsArrayType(string typeName, out string specialDelimiter, out string cleanTypeName)
        {
            specialDelimiter = null;
            cleanTypeName = null;
    
            if (string.IsNullOrEmpty(typeName)) {
                return false;
            }

            Match match = ArrayTypeRegex.Match(typeName);
    
            if (!match.Success) {
                return false;
            }

            string baseType = match.Groups[1].Value;
            string bracketContent = match.Groups[2].Value;
    
            cleanTypeName = baseType;
    
            if (string.IsNullOrEmpty(bracketContent)) {
                return true;
            }
    
            specialDelimiter = bracketContent;
            return true;
        }

        // TODO Shitcode, need to rewrite, current version is hard for understanding
        // TODO Need add ability to also use \n as delimiter by default
        public static string[] Tokenize(string input, string delimiter = null)
        {
            if (string.IsNullOrEmpty(input))
                return Array.Empty<string>();

            if (string.IsNullOrEmpty(delimiter))
                return new[] { input.Trim() };

            var tokens = new List<string>();
            int position = 0;
            bool inQuotes = false;
            var currentToken = new StringBuilder();
            string escapedDelimiter = Regex.Escape(delimiter);

            while (position < input.Length)
            {
                char currentChar = input[position];

                // Обробка escape-послідовностей
                if (currentChar == '\\' && position + 1 < input.Length)
                {
                    char nextChar = input[position + 1];
                    switch (nextChar)
                    {
                        case '\\': currentToken.Append('\\'); break;
                        case '"': currentToken.Append('"'); break;
                        case 'n': currentToken.Append('\n'); break;
                        case 'r': currentToken.Append('\r'); break;
                        case 't': currentToken.Append('\t'); break;
                        case 'b': currentToken.Append('\b'); break;
                        case 'f': currentToken.Append('\f'); break;
                        case '0': currentToken.Append('\0'); break;
                        default: currentToken.Append(nextChar); break; // якщо невідома послідовність
                    }

                    position += 2;
                    continue;
                }
                // Звичайна лапка (не екранована)
                else if (currentChar == '"')
                {
                    inQuotes = !inQuotes;
                    currentToken.Append(currentChar);
                }
                // Роздільник поза лапками
                else if (!inQuotes && position + delimiter.Length <= input.Length &&
                         Regex.IsMatch(input.Substring(position, delimiter.Length), "^" + escapedDelimiter + "$"))
                {
                    if (currentToken.Length > 0)
                    {
                        tokens.Add(currentToken.ToString().Trim());
                        currentToken.Clear();
                    }

                    position += delimiter.Length - 1;
                }
                else
                {
                    currentToken.Append(currentChar);
                }

                position++;
            }

            if (currentToken.Length > 0)
            {
                tokens.Add(currentToken.ToString().Trim());
            }

            // Фінальна обробка токенів
            for (int i = 0; i < tokens.Count; i++)
            {
                tokens[i] = ProcessToken(tokens[i]);
            }

            return tokens.ToArray();
        }

        private static string ProcessToken(string token)
        {
            if (string.IsNullOrEmpty(token))
                return token;

            // Якщо токен в лапках
            if (token.Length >= 2 && token.StartsWith("\"") && token.EndsWith("\""))
            {
                // Видаляємо зовнішні лапки і повертаємо внутрішній вміст
                return token.Substring(1, token.Length - 2);
            }

            return token;
        }
        
        private static ValueTableData GetValueTableData(int startRow, int startCol, string name,
            IList<IList<object>> pageData)
        {
            ValueTableData valueTableData = new ValueTableData()
            {
                Name = name,
                StartRow = startRow,
                StartCol = startCol,
            };

            int checkRow = startRow + 1;
            
            while (TryGetValueTableDataItem(checkRow, startCol, pageData, out var dataItem)) {
                checkRow = dataItem.Row + dataItem.Height;
                
                if (dataItem.Id.StartsWith('!'))  {
                    continue;
                }
                
                dataItem.Id = ExtractFieldName(dataItem.Id);

                valueTableData.Items.Add(dataItem);
            }

            // TODO need check, looks like we forgot about a comment line
            valueTableData.EndCol = valueTableData.StartCol + 2;

            if (valueTableData.Items.Count > 0) {
                ValueTableDataItem lastDataValue = valueTableData.Items[^1];
                valueTableData.EndRow = lastDataValue.Row + lastDataValue.Height - 1;
            } else {
                valueTableData.EndRow = valueTableData.StartRow;
            }
            
            return valueTableData;
        }

        private static bool TryGetConstantTableDataItem(int startRow, int startCol, IList<IList<object>> pageData,
            out ConstantTableDataItem item)
        {
            item = new ConstantTableDataItem()
            {
                Row = startRow,
            };
            
            int keyCol = startCol;
            int valueCol = startCol + 1;
            int commentCol = startCol + 2;
            
            string? keyData = GetCellData(pageData, startRow, keyCol);

            if (string.IsNullOrWhiteSpace(keyData) || keyData.Equals("END")) {
                return false;
            }

            item.Name = keyData;
            item.StringValue = GetCellData(pageData, startRow, valueCol);
            item.Comment = GetCellData(pageData, startRow, commentCol);

            return true;
        }
        
        private static ConstantTableData GetConstantTableData(int startRow, int startCol, string name,
            IList<IList<object>> pageData)
        {
            ConstantTableData constantTableData = new ConstantTableData()
            {
                Name = name,
                StartRow = startRow,
                StartCol = startCol,
            };
            
            int checkRow = startRow + 1;

            while (TryGetConstantTableDataItem(checkRow, startCol, pageData, out var dataItem))
            {
                checkRow++;
                
                if (dataItem.Name.StartsWith('!'))  {
                    continue;
                }
                
                // We do it here, because during parsing ConstantTableDataItem, name can start with ! sign
                dataItem.Name = ExtractFieldName(dataItem.Name);
                
                constantTableData.Items.Add(dataItem);
            }

            int intId = 1;
            Dictionary<int, int> idToIndexMap = new();

            int GetNextValidId()
            {
                int id = intId;

                while (idToIndexMap.ContainsKey(id))
                {
                    id++;
                }

                return id;
            }
            
            for (var i = 0; i < constantTableData.Items.Count; i++)
            {
                ConstantTableDataItem item = constantTableData.Items[i];
                
                if (string.IsNullOrWhiteSpace(item.StringValue))
                {
                    int validId = GetNextValidId();
                    item.Value = validId;
                    idToIndexMap.Add(validId, i);
                    intId = validId + 1;
                    continue;
                }

                if (AvailableTypes.Int.Parse(item.StringValue, out var parsedId))
                {
                    int id = (int)parsedId;
                    if (idToIndexMap.TryGetValue(id, out int index))
                    {
                        idToIndexMap[id] = i;
                        int validId = GetNextValidId();

                        ConstantTableDataItem otherItem = constantTableData.Items[i];
                        otherItem.Value = validId;

                        idToIndexMap.Add(validId, index);
                        intId = validId + 1;
                    }
                    else
                    {
                        item.Value = id;
                        idToIndexMap.Add(id, i);
                    }
                }
                else
                {
                    int validId = GetNextValidId();
                    item.Value = validId;
                    idToIndexMap.Add(validId, i);
                    intId = validId + 1;
                    
                    Console.WriteLine($"Warning: used invalid int value \"{item.StringValue}\", " +
                                      $"for constant: \"{item.Name}\". " +
                                      $"Table: {constantTableData.Name}, " +
                                      $"Row: {item.Row + 1}, " +
                                      $"Col: {IndexToColumn(startCol + 1)}. " +
                                      $"Instead we use a next free value for this constant: \"{validId}\"");
                }
            }
            
            constantTableData.EndCol = startCol + 2; // comment col
            constantTableData.EndRow = constantTableData.Items.Count > 0 ? constantTableData.Items[^1].Row : startRow;
            
            return constantTableData;
        }

        private static DatabaseTableData GetDatabaseTableData(
            int startRow,
            int startCol,
            string name,
            IList<IList<object>> pageData)
        {
            var tableData = new DatabaseTableData
            {
                Name = name,
                StartRow = startRow,
                StartCol = startCol,
            };

            var root = new FieldNode() {
                Name = "Root",
                ColumnIndex = startCol,
            };
            
            int startingMaxHeight = int.MaxValue;
            
            if (!TryGetCellData(pageData, startRow + 1, startCol, out string idType) || string.IsNullOrWhiteSpace(idType)) {
                // If we don't set any type we decide that it is INT, and all data is one-rowed
                idType = AvailableTypes.Int.TypeName;
                startingMaxHeight = 1;
            }
            
            idType = ExtractTypeName(idType);
            
            bool isIntTypeId = idType == AvailableTypes.Int.TypeName;
            
            AddToTree(root, ["id"], idType, startCol, null);

            tableData.RootFieldNode = root;

            int checkCol = startCol + 1;
            tableData.EndCol = startCol;
            
            while (TryGetCellData(pageData, startRow, checkCol, out string fieldName))
            {
                if (string.IsNullOrWhiteSpace(fieldName)) {
                    break;
                }
                
                // We skip column if its name starts with sign '!'
                if (fieldName.StartsWith('!')) {
                    checkCol++;
                    continue;
                }
                
                fieldName = ExtractFieldName(fieldName);
                
                string[] fieldsPath = fieldName.Split('.');
            
                if (!TryGetCellData(pageData, startRow + 1, checkCol, out string typeName)) {
                    // When type is empty we decide that type is equal "string"
                    typeName = AvailableTypes.String.TypeName;
                }

                TryGetCellData(pageData, startRow - 1, checkCol, out string comment);
                AddToTree(root, fieldsPath, typeName, checkCol, string.IsNullOrWhiteSpace(comment) ? null : comment);
                tableData.EndCol = checkCol;
                
                checkCol++;
            }

            sortFieldNodes(root);
            setupBaseTypes(root);

            tableData.DataObjects = new List<DataObject>();
            
            int row = startRow + 2;

            while (row < pageData.Count) {
                string cellData = GetCellData(pageData, row, startCol);

                if (cellData.Equals("END")) {
                    break;
                }
                
                if (!isIntTypeId && string.IsNullOrWhiteSpace(cellData)) {
                    break;
                }

                DataObject obj = ParceObject(row, startingMaxHeight, root, pageData);

                if (!cellData.StartsWith('!')) {
                    tableData.DataObjects.Add(obj);
                }

                row += obj.Height;
            }
            
            if (isIntTypeId)
            {
                int intId = 1;
                Dictionary<int, int> idToIndexMap = new();
                int GetNextValidId()
                {
                    int id = intId;
        
                    while (idToIndexMap.ContainsKey(id))
                    {
                        id++;
                    }
        
                    return id;
                }
                for (var i = 0; i < tableData.DataObjects.Count; i++)
                {
                    DataObject item = tableData.DataObjects[i];
                    DataField idDataField = item.Fields[0];
                    
                    string? idValue = null;

                    if (idDataField.Values.Count > 0) {
                        idValue = idDataField.Values[0];
                    } else {
                        idDataField.Values.Add(string.Empty);
                    }
        
                    if (string.IsNullOrWhiteSpace(idValue))
                    {
                        int validId = GetNextValidId();
                        idDataField.Values[0] = validId.ToString();
                        idToIndexMap.Add(validId, i);
                        intId = validId + 1;
                        continue;
                    }
        
                    if (AvailableTypes.Int.Parse(idValue, out var parsedId))
                    {
                        int id = (int)parsedId;
                        if (idToIndexMap.TryGetValue(id, out int index))
                        {
                            idToIndexMap[id] = i;
                            int validId = GetNextValidId();
                            
                            DataObject otherItem = tableData.DataObjects[i];
                            DataField otherItemIdDataField = otherItem.Fields[0];
                            
                            otherItemIdDataField.Values[0] = validId.ToString();
                            
                            idToIndexMap.Add(validId, index);
                            intId = validId + 1;
                        }
                        else
                        {
                            idDataField.Values[0] = id.ToString();
                            idToIndexMap.Add(id, i);
                        }
                    }
                }
            }

            if (tableData.DataObjects.Count > 0) {
                DataObject lastItem = tableData.DataObjects[^1];
                tableData.EndRow = lastItem.RowIndex + lastItem.Height - 1;
            } else {
                tableData.EndRow = tableData.StartRow + 1;
            }

            return tableData;
        }

        private static DataObject ParceObject(int row, int maxHeight, FieldNode fieldNode,
            IList<IList<object>> pageData)
        {
            var obj = new DataObject {
                RowIndex = row,
                Height = maxHeight,
                ColumnIndex = fieldNode.ColumnIndex
            };
            
            bool hasHeight = false;
            int checkRow = row;

            foreach (FieldNode child in fieldNode.Children)
            {
                if (child.Children.Count == 0)
                {
                    DataField dataField = GetFieldData(checkRow, obj.Height, child, pageData);

                    if (!hasHeight) {
                        obj.Height = dataField.Height;
                        hasHeight = true;
                    }
                    
                    obj.Fields.Add(dataField);
                }
                else
                {
                    // Вкладений об'єкт / об'єкти
                    DataArray array = ParseArray(child, checkRow, obj.Height, pageData);
                    obj.Arrays.Add(array);
                }
            }

            return obj;
        }

        private static DataArray ParseArray(
            FieldNode arrayNode,
            int startRow,
            int maxHeight,
            IList<IList<object>> pageData)
        {
            var array = new DataArray
            {
                Name = arrayNode.Name,
                RowIndex = startRow,
                ColumnIndex = arrayNode.ColumnIndex,
                Height = maxHeight,
            };

            int checkRow = startRow;
            
            while (checkRow < startRow + maxHeight)
            {
                int height = startRow + maxHeight - checkRow;
                DataObject obj = ParceObject(checkRow, height, arrayNode, pageData);

                array.Items.Add(obj);
                
                checkRow += obj.Height;
            }

            return array;
        }

        private static DataField GetFieldData(int row, int maxHeight, FieldNode field,
            IList<IList<object>> pageData)
        {
            DataField dataField = new DataField()
            {
                Name = field.Name,
                RowIndex = row,
                ColumnIndex = field.ColumnIndex,
                ValuesRows = new List<int>()
            };
            
            int height = 0;
            int checkRow = row;

            while (checkRow < pageData.Count)
            {
                if (height >= maxHeight) {
                    break;
                }

                string value = GetCellData(pageData, checkRow, field.ColumnIndex);

                if (!string.IsNullOrWhiteSpace(value)) {
                    if (value.Equals("END")) {
                        break;
                    }
                    
                    if (field.ArrayType == ArrayType.Multicell) {
                        dataField.Values.Add(value);
                    } else {
                        if (dataField.Values.Count > 0) {
                            break;
                        }

                        if (field.ArrayType == ArrayType.OneCell) {
                            string[] tokens = Tokenize(value, field.ArrayDelimiter);
                            dataField.Values.AddRange(tokens);
                        }
                        else {
                            dataField.Values.Add(value);
                        }
                    }
                    
                    dataField.ValuesRows.Add(checkRow);
                }
                
                checkRow++;
                height++;
            }
            
            dataField.Height = height;
            
            return dataField;
        }

        private static void sortFieldNodes(FieldNode fieldNode)
        {
            fieldNode.Children = fieldNode.Children.OrderBy(node => node.Children.Count > 0).ToList();

            foreach (FieldNode child in fieldNode.Children) {
                sortFieldNodes(child);
            }
        }
        
        private static void setupBaseTypes(FieldNode fieldNode)
        {
            bool hasBaseType = !string.IsNullOrWhiteSpace(fieldNode.BaseType);
            string? childCustomType = null;
            
            foreach (FieldNode child in fieldNode.Children) {
                setupBaseTypes(child);

                if (!hasBaseType && childCustomType == null) {
                    childCustomType = child.CustomType;
                }
            }

            if (!hasBaseType) {
                fieldNode.BaseType = childCustomType ?? $"{fieldNode.Name}Type";
            }
        }

        private static void AddToTree(FieldNode node, string[] path, string typeDef, int columnIndex, string? comment,
            int index = 0)
        {
            string current = path[index];
            FieldNode? child = node.Children.FirstOrDefault(c => c.Name == current);

            bool isLastIndex = index == path.Length - 1;

            if (isLastIndex && child != null && child.Children.Count > 0) {
                child = null;
            }
            
            if (child == null)
            {
                child = new FieldNode
                {
                    Name = ExtractFieldName(current),
                    ColumnIndex = columnIndex,
                };
                
                node.Children.Add(child);
            }

            if (isLastIndex) {
                // кінець шляху → визначаємо тип
                string? customType = null;
                string[] typeParts = typeDef.Split('.', ':');

                if (typeParts.Length > 1) {
                    customType = ExtractTypeName(typeParts[0]);
                    typeDef = typeParts[1];
                }

                if (string.IsNullOrWhiteSpace(typeDef)) {
                    typeDef = AvailableTypes.String.TypeName;
                }
                
                bool isArray = IsArrayType(typeDef, out string delimiter, out string cleanTypeName);

                if (isArray) {
                    typeDef = cleanTypeName;

                    if (string.IsNullOrWhiteSpace(delimiter)) {
                        child.ArrayType = ArrayType.Multicell;
                    } else {
                        child.ArrayType = ArrayType.OneCell;
                        child.ArrayDelimiter = delimiter;
                    }
                }
                
                child.BaseType = ExtractTypeName(typeDef);
                child.CustomType = customType;
                child.Comment = comment;
                child.ColumnIndex = columnIndex;
            }
            else
            {
                AddToTree(child, path, typeDef, columnIndex, comment, index + 1);
            }
        }

        private static bool ValidateTablesByDuplicatesInNames(List<TableData> tableDataList)
        {
            bool isValid = true;
            
            foreach (var tableData in tableDataList)
            {
                if (tableData is ValueTableData valueTableData)
                {
                    Dictionary<string, int> idToRowMap = new();
                    
                    foreach (var data in valueTableData.Items)
                    {
                        if (idToRowMap.TryGetValue(data.Id, out var row))
                        {
                            Console.WriteLine($"Table \"{tableData.Name}\" has duplicates for id \"{data.Id}\": " +
                                              $"Row [{row + 1} and {data.Row + 1}], " +
                                              $"Col [{IndexToColumn(valueTableData.StartCol)}]");
                            
                            isValid = false;
                        }
                        else
                        {
                            idToRowMap.Add(data.Id, data.Row);
                        }
                    }
                }
                else if (tableData is DatabaseTableData databaseTableData)
                {
                    Dictionary<string, int> idToRowMap = new();

                    foreach (DataObject dataObject in databaseTableData.DataObjects)
                    {
                        DataField idDataField = dataObject.Fields[0];
                        string id = idDataField.Values[0];
                        
                        if (idToRowMap.TryGetValue(id, out var row))
                        {
                            isValid = false;
                            Console.WriteLine($"Table \"{tableData.Name}\" has duplicates for id \"{id}\": " +
                                              $"Row [{row + 1} and {idDataField.RowIndex + 1}], " +
                                              $"Col [{IndexToColumn(databaseTableData.StartCol)}]");
                        }
                        else
                        {
                            idToRowMap.Add(id, idDataField.RowIndex);
                        }
                    }

                    if (!IsValidFieldNodeByNameDuplicates(databaseTableData.RootFieldNode, databaseTableData)) {
                        isValid = false;
                    }
                }
                else if (tableData is ConstantTableData constantTableData)
                {
                    Dictionary<string, int> idToRowMap = new();
                    
                    foreach (var data in constantTableData.Items)
                    {
                        if (idToRowMap.TryGetValue(data.Name, out var row))
                        {
                            Console.WriteLine($"Table \"{tableData.Name}\" has duplicates for names \"{data.Name}\": " +
                                              $"Row [{row + 1} and {data.Row + 1}], " +
                                              $"Col [{IndexToColumn(constantTableData.StartCol)}]");
                            
                            isValid = false;
                        }
                        else
                        {
                            idToRowMap.Add(data.Name, data.Row);
                        }
                    }
                }
            }
        
            return isValid;
        }
        
        private static bool IsValidFieldNodeByNameDuplicates(FieldNode fieldNode, DatabaseTableData databaseTableData)
        {
            bool isValid = true;

            Dictionary<string, FieldNode> fieldNameToFieldNodeMap = new();
            Dictionary<string, FieldNode> fieldInnerTypeToFieldNodeMap = new();
            
            foreach (FieldNode child in fieldNode.Children)
            {
                if (fieldNameToFieldNodeMap.TryGetValue(child.Name, out FieldNode? duplicateFieldNode)) 
                {
                    isValid = false;
                    
                    Console.WriteLine($"Table \"{databaseTableData.Name}\" has duplicates for field name \"{child.Name}\": " +
                                      $"Row [{databaseTableData.StartRow + 1}], " +
                                      $"Col [{IndexToColumn(duplicateFieldNode.ColumnIndex)} and {IndexToColumn(child.ColumnIndex)}]");
                }
                else
                {
                    fieldNameToFieldNodeMap.Add(child.Name, child);
                }

                if (child.Children.Count == 0) {
                    continue;
                }

                if (fieldNode.BaseType == child.BaseType)
                {
                    isValid = false;
                    
                    Console.WriteLine($"Table \"{databaseTableData.Name}\" has problem for inner type name \"{child.BaseType}\". " +
                                      "Inner type can't be the same as outer type. " +
                                      $"Row [{databaseTableData.StartRow + 2}], " +
                                      $"Col [{IndexToColumn(fieldNode.ColumnIndex)} and {IndexToColumn(child.ColumnIndex)}]");
                }

                if (fieldInnerTypeToFieldNodeMap.TryGetValue(child.BaseType, out duplicateFieldNode))
                {
                    isValid = false;
                    
                    Console.WriteLine($"Table \"{databaseTableData.Name}\" has duplicates for inner type name \"{child.BaseType}\": " +
                                      $"Row [{databaseTableData.StartRow + 2}], " +
                                      $"Col [{IndexToColumn(duplicateFieldNode.ColumnIndex)} and {IndexToColumn(child.ColumnIndex)}]");
                }
                else
                {
                    fieldInnerTypeToFieldNodeMap.Add(child.BaseType, child);
                }

                if (!IsValidFieldNodeByNameDuplicates(child, databaseTableData))
                {
                    isValid = false;
                }
            }
            
            return isValid;
        }
        
        
        private static bool IsValidFieldNodeByNamePatterns(FieldNode fieldNode, DatabaseTableData databaseTableData)
        {
            bool isValid = true;

            if (!IsValidTypeName(fieldNode.BaseType)) {
                isValid = false;
                
                Console.WriteLine($"Table \"{databaseTableData.Name}\" has invalid name for type \"{fieldNode.BaseType}\": " +
                                  $"Row [{databaseTableData.StartRow + 2}], " +
                                  $"Col [{IndexToColumn(fieldNode.ColumnIndex)}]");
            }

            if (!IsValidFieldName(fieldNode.Name)) {
                isValid = false;
                
                Console.WriteLine($"Table \"{databaseTableData.Name}\" has invalid name for field \"{fieldNode.Name}\": " +
                                  $"Row [{databaseTableData.StartRow + 1}], " +
                                  $"Col [{IndexToColumn(fieldNode.ColumnIndex)}]");
            }

            foreach (FieldNode child in fieldNode.Children) {
                if (!IsValidFieldNodeByNamePatterns(child, databaseTableData)) {
                    isValid = false;
                }
            }

            return isValid;
        }
        
        private static bool ValidateTablesByNamePatterns(List<TableData> tableDataList)
        {
            bool isValid = true;
            
            foreach (var tableData in tableDataList)
            {
                if (!IsValidTypeName(tableData.Name))
                {
                    isValid = false;
        
                    Console.WriteLine($"Table \"{tableData.Name}\" has invalid name");
                }
                
                if (tableData is ValueTableData valueTableData)
                {
                    foreach (var data in valueTableData.Items)
                    {
                        if (!IsValidFieldName(data.Id))
                        {
                            isValid = false;
                            
                            Console.WriteLine($"Table \"{tableData.Name}\" has invalid name for id \"{data.Id}\": " +
                                              $"Row [{data.Row + 1}], " +
                                              $"Col [{IndexToColumn(valueTableData.StartCol)}]");
                        }
                        
                        if (!IsValidTypeName(data.Type))
                        {
                            isValid = false;
                            
                            Console.WriteLine($"Table \"{tableData.Name}\" has invalid name for type \"{data.Type}\": " +
                                              $"Row [{data.Row + 1}], " +
                                              $"Col [{IndexToColumn(valueTableData.StartCol + 1)}]");
                        }
                    }
                }
                else if (tableData is DatabaseTableData databaseTableData)
                {
                    foreach (FieldNode childNode in databaseTableData.RootFieldNode.Children)
                    {
                        if (!IsValidFieldNodeByNamePatterns(childNode, databaseTableData)) {
                            isValid = false;
                        }
                    }
                }
                else if (tableData is ConstantTableData constantTableData)
                {
                    foreach (var data in constantTableData.Items)
                    {
                        if (!IsValidFieldName(data.Name))
                        {
                            isValid = false;
                            
                            Console.WriteLine($"Table \"{tableData.Name}\" has invalid name \"{data.Name}\": " +
                                              $"Row [{data.Row + 1}], " +
                                              $"Col [{IndexToColumn(constantTableData.StartCol)}]");
                        }
                    }
                }
            }
        
            return isValid;
        }

        public static string IndexToColumn(int number)
        {
            string columnName = "";
            while (number >= 0)
            {
                columnName = (char)('A' + (number % 26)) + columnName;
                number = (number / 26) - 1;
            }
            return columnName;
        }
        
        private static bool ValidateTablesByOverlapping(List<TableData> tableDataList)
        {
            List<(TableData, TableData)> overlappedTables = new();
            
            foreach (var table1 in tableDataList)
            {
                foreach (var table2 in tableDataList)
                {
                    if (table1 == table2)
                    {
                        continue;
                    }
                    
                    if (AreTablesOverlap(table1, table2))
                    {
                        if (!overlappedTables.Contains((table1, table2)) &&
                            !overlappedTables.Contains((table2, table1)))
                        {
                            overlappedTables.Add((table1, table2));
                        }
                    }
                }
            }
            
            foreach (var tables in overlappedTables)
            {
                Console.WriteLine($"Tables \"{tables.Item1.Name}\" and {tables.Item2.Name} overlap.");
            }
            
            return overlappedTables.Count == 0;
        }

        private static bool AreTablesOverlap(TableData table1, TableData table2)
        {
            Rect table1RectWithSafeZone = new Rect(
                new Vector2(table1.StartCol - 1, table1.StartRow - 1), 
                new Vector2(table1.EndCol + 1, table1.EndRow + 1));
            
            Rect table2Rect = new Rect(
                new Vector2(table2.StartCol, table2.StartRow), 
                new Vector2(table2.EndCol, table2.EndRow));

            return table1RectWithSafeZone.Overlaps(table2Rect);
        }

        private static bool IsValidTypeName(string value)
        {
            const string dataTypePattern = @"^([A-Za-z][A-Za-z0-9]*)(\[\])?$";

            if (value.Equals("id", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }
            
            return Regex.IsMatch(value, dataTypePattern);
        }
        
        private static bool IsValidFieldName(string value)
        {
            const string fieldNamePattern = @"^([A-Za-z_][A-Za-z0-9_]*)$";
            
            return Regex.IsMatch(value, fieldNamePattern);
        }

        private static string ExtractTypeName(string value)
        {
            string result = value;
            
            if (value.StartsWith('$')) {
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
            string result = new string(value.Where(c => !char.IsWhiteSpace(c)).ToArray());
            return result;
        }
    }
}
