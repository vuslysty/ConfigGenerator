using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Text.RegularExpressions;
using ConfigGenerator.ConfigInfrastructure;
using ConfigGenerator.ConfigInfrastructure.Data;
using Humanizer;

namespace ConfigGenerator
{
    public static class TableDataUtilities
    {
        private const string TableStartPattern = @"^#([A-Za-z][A-Za-z0-9 ]*)$";

        public static bool ExtractTablesFromPage(string pageName, IList<IList<object>> pageData, out List<TableData> tableDataList)
        {
            tableDataList = new();
            
            var possibleTables = GetPossibleTables(pageName, pageData);
            
            foreach (var possibleTable in possibleTables)
            {
                int startRow = possibleTable.row;
                int startCol = possibleTable.col;
                
                string tableName = ExtractTypeName(possibleTable.name);

                if (GetCellData(pageData, startRow, startCol + 1) == "type" &&
                    GetCellData(pageData, startRow, startCol + 2) == "value")
                {
                    ValueTableData valueTableData = GetValueTableData(startRow, startCol, tableName, pageData);
                    tableDataList.Add(valueTableData);
                }
                else
                {
                    DatabaseTableData databaseTableData = GetDatabaseTableData(startRow, startCol, tableName, pageData);
                    tableDataList.Add(databaseTableData);
                }
            }

            if (!ValidateTablesByOverlapping(tableDataList))
            {
                return false;
            }
            
            // if (!ValidateTablesByNamePatterns(tableDataList))
            // {
            //     return false;
            // }
            
            // if (!ValidateTablesByDuplicatesInNames(tableDataList))
            // {
            //     return false;
            // }
            
            return true;
        }

        public static bool ValidateTableTypesAndValues(TableData tableData, AvailableTypes availableTypes)
        {
            bool isValid = true;
            
            switch (tableData)
            {
                case ValueTableData valueTableData:
                    foreach (ValueTableDataItem dataValue in valueTableData.DataValues)
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
                            for (var i = 0; i < dataValue.Values.Count; i++) {
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
                        } else {
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
                
                // case DatabaseTableData databaseTableData:
                //     var idTypeDescriptor = availableTypes.GetTypeDescriptor(databaseTableData.IdType);
                //
                //     if (idTypeDescriptor == null || 
                //         (databaseTableData.IdType != "string" && databaseTableData.IdType != "int"))
                //     {
                //         isValid = false;
                //         Console.WriteLine($"Error: used invalid data type \"{databaseTableData.IdType}\" for id. " +
                //                           $"Only valid types for id: \"string\" or \"int\". " +
                //                           $"Table: {databaseTableData.Name}, " +
                //                           $"Row: {databaseTableData.StartRow + 2}, " +
                //                           $"Col: {IndexToColumn(databaseTableData.StartCol)}.");
                //     }
                //     else
                //     {
                //         // Validate id values
                //         foreach (var lineData in databaseTableData.ValueLines)
                //         {
                //             if (!idTypeDescriptor.Parse(lineData.Id, out var parsedValue))
                //             {
                //                 isValid = false;
                //                 Console.WriteLine($"Error: used invalid data value \"{lineData.Id}\", " +
                //                                   $"for type: \"{idTypeDescriptor.TypeName}\". " +
                //                                   $"Table: {databaseTableData.Name}, " +
                //                                   $"Row: {lineData.Row + 1}, " +
                //                                   $"Col: {IndexToColumn(databaseTableData.StartCol)}.");
                //             }
                //         }
                //     }
                //
                //     for (var fieldIndex = 0; fieldIndex < databaseTableData.FieldDescriptors.Count; fieldIndex++)
                //     {
                //         var fieldDescriptor = databaseTableData.FieldDescriptors[fieldIndex];
                //         var typeDescriptor = availableTypes.GetTypeDescriptor(fieldDescriptor.TypeName);
                //
                //         if (typeDescriptor == null)
                //         {
                //             isValid = false;
                //             Console.WriteLine($"Error: used invalid data type \"{fieldDescriptor.TypeName}\". " +
                //                               $"Table: {databaseTableData.Name}, " +
                //                               $"Row: {databaseTableData.StartRow + 2}, " +
                //                               $"Col: {IndexToColumn(fieldDescriptor.Col)}.");
                //             continue;
                //         }
                //
                //         foreach (var valuesLine in databaseTableData.ValueLines)
                //         {
                //             var valueStr = valuesLine.Values[fieldIndex];
                //
                //             if (!typeDescriptor.Parse(valueStr, out var parsedValue))
                //             {
                //                 isValid = false;
                //                 Console.WriteLine($"Error: used invalid data value \"{valueStr}\", " +
                //                                   $"for type \"{fieldDescriptor.TypeName}\". " +
                //                                   $"Table: {databaseTableData.Name}, " +
                //                                   $"Row: {valuesLine.Row + 1}, " +
                //                                   $"Col: {IndexToColumn(fieldDescriptor.Col)}.");
                //             }
                //         }
                //     }
                //
                //     break;
                default:
                    Console.WriteLine("Error: tried to validate an unsupported table type");
                    isValid = false;
                    break;
            }

            return isValid;
        }

        private static List<(string name, int row, int col)> GetPossibleTables(
            string pageName,
            IList<IList<object>> pageData)
        {
            List<(string name, int row, int col)> possibleTables = new();

            for (int row = 0; row < pageData.Count; row++)
            {
                int columnCount = pageData[row].Count;

                for (int col = 0; col < columnCount; col++)
                {
                    string cellData = (string)pageData[row][col];

                    if (row == 0 && col == 0 && (cellData == "id" || cellData == string.Empty))
                    {
                        if (cellData == "id")
                        {
                            possibleTables.Add((pageName, row, col));
                        }
                        else
                        {
                            if (TryGetCellData(pageData, row + 1, col, out string nextCellData))
                            {
                                if (nextCellData == "id")
                                {
                                    possibleTables.Add((pageName, row + 1, col));
                                }
                            }
                        }
                    }
                    else if (cellData.StartsWith('#') && Regex.IsMatch(cellData, TableStartPattern))
                    {
                        string tableName = cellData.Substring(1).Trim();
                        
                        // Data in the cell below the table name exists
                        if (TryGetCellData(pageData, row + 1, col, out string nextCellData))
                        {
                            if (nextCellData == "id")
                            {
                                possibleTables.Add((tableName, row + 1, col));
                            }
                            else if (nextCellData == "")
                            {
                                if (TryGetCellData(pageData, row + 2, col, out nextCellData))
                                {
                                    if (nextCellData == "id")
                                    {
                                        possibleTables.Add((tableName, row + 2, col));
                                    }
                                }
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

        private static string GetCellData(IList<IList<object>> pageData, int row, int col)
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

            string idData = GetCellData(pageData, checkRow, idCol);

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
            
            item.Type = string.IsNullOrWhiteSpace(item.Type) 
                ? AvailableTypes.String.TypeName 
                : ExtractTypeName(item.Type);

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

                valueTableData.DataValues.Add(dataItem);
            }

            valueTableData.EndCol = valueTableData.StartCol + 2;

            if (valueTableData.DataValues.Count > 0) {
                ValueTableDataItem lastDataValue = valueTableData.DataValues[^1];
                valueTableData.EndRow = lastDataValue.Row + lastDataValue.Height - 1;
            } else {
                valueTableData.EndRow = valueTableData.StartRow;
            }
            
            return valueTableData;
        }

        // private static DatabaseTableData GetDatabaseTableDataOLD(int startRow, int startCol, string name,
        //     IList<IList<object>> pageData)
        // {
        //     DatabaseTableData databaseTableData = new DatabaseTableData
        //     {
        //         Name = name,
        //         StartRow = startRow,
        //         StartCol = startCol,
        //     };
        //
        //     int checkCol = startCol + 1;
        //     while (TryGetCellData(pageData, startRow, checkCol, out string fieldName) && fieldName != "")
        //     {
        //         // We skip column if its name starts with sign '!'
        //         if (fieldName.StartsWith('!'))
        //         {
        //             checkCol++;
        //             continue;
        //         }
        //         
        //         fieldName = ExtractFieldName(fieldName);
        //         
        //         DatabaseTableFieldDescriptorItem typeItem = new DatabaseTableFieldDescriptorItem()
        //         {
        //             FieldName = fieldName,
        //             Col = checkCol
        //         };
        //
        //         if (!TryGetCellData(pageData, startRow + 1, checkCol, out string typeName))
        //         {
        //             // When type is empty we decide that type is equal "string"
        //             typeName = "string";
        //         }
        //         
        //         typeName = ExtractTypeName(typeName);
        //         typeItem.TypeName = typeName;
        //
        //         if (!TryGetCellData(pageData, startRow - 1, checkCol, out string comment))
        //         {
        //             comment = string.Empty;
        //         }
        //         
        //         typeItem.Comment = comment;
        //
        //         databaseTableData.FieldDescriptors.Add(typeItem);
        //
        //         checkCol++;
        //     }
        //
        //     if (!TryGetCellData(pageData, startRow + 1, startCol, out string idType))
        //     {
        //         idType = "int";
        //     }
        //
        //     idType = idType.Trim();
        //     databaseTableData.IdType = idType;
        //
        //     int checkDataRow = startRow + 2;
        //     
        //     while (true)
        //     {
        //         int notEmptyDataCounter = 0;
        //
        //         string id = GetCellData(pageData, checkDataRow, startCol);
        //
        //         if (string.IsNullOrWhiteSpace(id))
        //         {
        //             id = string.Empty;
        //         }
        //         else
        //         {
        //             notEmptyDataCounter++;
        //         }
        //
        //         if (id.StartsWith('!'))
        //         {
        //             checkDataRow++;
        //             continue;
        //         }
        //
        //         DatabaseTableValuesLineData? lineData = new DatabaseTableValuesLineData()
        //         {
        //             Id = id,
        //             Row = checkDataRow,
        //         };
        //
        //         foreach (var dataType in databaseTableData.FieldDescriptors)
        //         {
        //             if (!TryGetCellData(pageData, checkDataRow, dataType.Col, out string dataContent))
        //             {
        //                 dataContent = string.Empty;
        //             }
        //
        //             if (dataContent != string.Empty)
        //             {
        //                 notEmptyDataCounter++;
        //             }
        //
        //             lineData.Values.Add(dataContent);
        //         }
        //
        //         if (notEmptyDataCounter > 0)
        //         {
        //             checkDataRow++;
        //             databaseTableData.ValueLines.Add(lineData);
        //         }
        //         else
        //         {
        //             break;
        //         }
        //     }
        //
        //     if (AvailableTypes.Int.TypeName == databaseTableData.IdType)
        //     {
        //         int intId = 1;
        //         Dictionary<int, int> idToIndexMap = new();
        //
        //         int GetNextValidId()
        //         {
        //             int id = intId;
        //             
        //             while (idToIndexMap.ContainsKey(id))
        //             {
        //                 id++;
        //             }
        //             
        //             return id;
        //         }
        //
        //         for (var i = 0; i < databaseTableData.ValueLines.Count; i++)
        //         {
        //             var lineData = databaseTableData.ValueLines[i];
        //             
        //             if (string.IsNullOrWhiteSpace(lineData.Id))
        //             {
        //                 int validId = GetNextValidId();
        //                 lineData.Id = validId.ToString();
        //                 idToIndexMap.Add(validId, i);
        //                 intId = validId + 1;
        //                 continue;
        //             }
        //             
        //             if (AvailableTypes.Int.Parse(lineData.Id, out var parsedId))
        //             {
        //                 int id = (int)parsedId;
        //
        //                 if (idToIndexMap.TryGetValue(id, out int index))
        //                 {
        //                     idToIndexMap[id] = i;
        //                     int validId = GetNextValidId();
        //                     databaseTableData.ValueLines[index].Id = validId.ToString();
        //                     idToIndexMap.Add(validId, index);
        //                     intId = validId + 1;
        //                 }
        //                 else
        //                 {
        //                     idToIndexMap.Add(id, i);
        //                 }
        //             }
        //         }
        //     }
        //
        //     databaseTableData.EndCol = databaseTableData.FieldDescriptors.Count > 0
        //         ? databaseTableData.FieldDescriptors[^1].Col
        //         : databaseTableData.StartCol;
        //
        //     databaseTableData.EndRow = databaseTableData.ValueLines.Count > 0
        //         ? databaseTableData.StartRow + databaseTableData.ValueLines.Count + 1
        //         : databaseTableData.StartRow + 1;
        //
        //     return databaseTableData;
        // }

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
                Name = "Root"
            };
            
            if (!TryGetCellData(pageData, startRow + 1, startCol, out string idType)) {
                idType = AvailableTypes.Int.TypeName;
            }
            
            bool isIntTypeId = idType == AvailableTypes.Int.TypeName;
            
            idType = ExtractTypeName(idType);
            
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
                
                typeName = ExtractTypeName(typeName);

                TryGetCellData(pageData, startRow - 1, checkCol, out string comment);
                AddToTree(root, fieldsPath, typeName, checkCol, string.IsNullOrWhiteSpace(comment) ? null : comment);
                tableData.EndCol = checkCol;
                
                checkCol++;
            }

            sortFieldNodes(root);
            setupBaseTypes(root);

            tableData.DataObjects = new List<DataObject>();

            int startingMaxHeight = isIntTypeId ? 1 : int.MaxValue;
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
                tableData.DataObjects.Add(obj);
                
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
                fieldNode.BaseType = childCustomType ?? fieldNode.Name;
            }
        }

        private static void AddToTree(FieldNode node, string[] path, string typeDef, int columnIndex, string? comment,
            int index = 0)
        {
            string current = path[index];
            
            FieldNode? child = node.Children.FirstOrDefault(c => c.Name == current);
            
            if (child == null) {
                child = new FieldNode { Name = ExtractFieldName(current) };
                node.Children.Add(child);
            }

            if (index == path.Length - 1) {
                // кінець шляху → визначаємо тип
                string? customType = null;
                string[] typeParts = typeDef.Split('.', ':');

                if (typeParts.Length > 1) {
                    customType = typeParts[0];
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
                
                child.BaseType = typeDef;
                child.CustomType = customType;
                child.Comment = comment;
                child.ColumnIndex = columnIndex;
            }
            else
            {
                AddToTree(child, path, typeDef, columnIndex, comment, index + 1);
            }
        }

        // private static bool ValidateTablesByDuplicatesInNames(List<TableData> tableDataList)
        // {
        //     bool isValid = true;
        //     
        //     foreach (var tableData in tableDataList)
        //     {
        //         if (tableData is ValueTableData valueTableData)
        //         {
        //             Dictionary<string, int> idToRowMap = new();
        //             
        //             foreach (var data in valueTableData.DataValues)
        //             {
        //                 if (idToRowMap.TryGetValue(data.Id, out var row))
        //                 {
        //                     Console.WriteLine($"Table \"{tableData.Name}\" has duplicates for id \"{data.Id}\": " +
        //                                       $"Row [{row + 1} and {data.Row + 1}], " +
        //                                       $"Col [{IndexToColumn(valueTableData.StartCol)}]");
        //                     
        //                     isValid = false;
        //                 }
        //                 else
        //                 {
        //                     idToRowMap.Add(data.Id, data.Row);
        //                 }
        //             }
        //         }
        //         else if (tableData is DatabaseTableData databaseTableData)
        //         {
        //             Dictionary<string, int> idToRowMap = new();
        //             
        //             for (var i = 0; i < databaseTableData.ValueLines.Count; i++)
        //             {
        //                 var lineData = databaseTableData.ValueLines[i];
        //                 var id = lineData.Id;
        //                 
        //                 if (idToRowMap.TryGetValue(id, out var row))
        //                 {
        //                     isValid = false;
        //                     Console.WriteLine($"Table \"{tableData.Name}\" has duplicates for id \"{id}\": " +
        //                                     $"Row [{row + 1} and {lineData.Id + 1}], " +
        //                                     $"Col [{IndexToColumn(databaseTableData.StartCol)}]");
        //                 }
        //                 else
        //                 {
        //                     idToRowMap.Add(id, lineData.Row);
        //                 }
        //             }
        //
        //             Dictionary<string, int> nameToColMap = new();
        //             
        //             foreach (var data in databaseTableData.FieldDescriptors)
        //             {
        //                 if (nameToColMap.TryGetValue(data.FieldName, out var col))
        //                 {
        //                     isValid = false;
        //                     
        //                     Console.WriteLine($"Table \"{tableData.Name}\" has duplicates for field name \"{data.FieldName}\": " +
        //                                       $"Row [{databaseTableData.StartRow + 1}], " +
        //                                       $"Col [{IndexToColumn(col)} and {IndexToColumn(data.Col)}]");
        //                 }
        //                 else
        //                 {
        //                     nameToColMap.Add(data.FieldName, data.Col);
        //                 }
        //             }
        //         }
        //     }
        //
        //     return isValid;
        // }
        
        // private static bool ValidateTablesByNamePatterns(List<TableData> tableDataList)
        // {
        //     bool isValid = true;
        //     
        //     foreach (var tableData in tableDataList)
        //     {
        //         if (!IsValidTypeName(tableData.Name))
        //         {
        //             isValid = false;
        //
        //             Console.WriteLine($"Table \"{tableData.Name}\" has invalid name");
        //         }
        //         
        //         if (tableData is ValueTableData valueTableData)
        //         {
        //             foreach (var data in valueTableData.DataValues)
        //             {
        //                 if (!IsValidFieldName(data.Id))
        //                 {
        //                     isValid = false;
        //                     
        //                     Console.WriteLine($"Table \"{tableData.Name}\" has invalid name for id \"{data.Id}\": " +
        //                                       $"Row [{data.Row + 1}], " +
        //                                       $"Col [{IndexToColumn(valueTableData.StartCol)}]");
        //                 }
        //                 
        //                 if (!IsValidTypeName(data.Type))
        //                 {
        //                     isValid = false;
        //                     
        //                     Console.WriteLine($"Table \"{tableData.Name}\" has invalid name for type \"{data.Type}\": " +
        //                                       $"Row [{data.Row + 1}], " +
        //                                       $"Col [{IndexToColumn(valueTableData.StartCol + 1)}]");
        //                 }
        //             }
        //         }
        //         else if (tableData is DatabaseTableData databaseTableData)
        //         {
        //             if (!IsValidTypeName(databaseTableData.IdType))
        //             {
        //                 isValid = false;
        //                 
        //                 Console.WriteLine($"Table \"{tableData.Name}\" has invalid name for id type \"{databaseTableData.IdType}\": " +
        //                                   $"Row [{databaseTableData.StartRow + 1}], " +
        //                                   $"Col [{IndexToColumn(databaseTableData.StartCol)}]");
        //             }
        //             
        //             foreach (var fieldDescriptor in databaseTableData.FieldDescriptors)
        //             {
        //                 if (!IsValidTypeName(fieldDescriptor.TypeName))
        //                 {
        //                     isValid = false;
        //                     
        //                     Console.WriteLine($"Table \"{tableData.Name}\" has invalid name for type \"{fieldDescriptor.TypeName}\": " +
        //                                       $"Row [{databaseTableData.StartRow + 2}], " +
        //                                       $"Col [{IndexToColumn(fieldDescriptor.Col)}]");
        //                 }
        //
        //                 if (!IsValidFieldName(fieldDescriptor.FieldName))
        //                 {
        //                     isValid = false;
        //                     
        //                     Console.WriteLine($"Table \"{tableData.Name}\" has invalid name for field \"{fieldDescriptor.FieldName}\": " +
        //                                       $"Row [{databaseTableData.StartRow + 1}], " +
        //                                       $"Col [{IndexToColumn(fieldDescriptor.Col)}]");
        //                 }
        //             }
        //
        //             if (AvailableTypes.String.TypeName == databaseTableData.IdType)
        //             {
        //                 foreach (var lineData in databaseTableData.ValueLines)
        //                 {
        //                     if (!IsValidFieldName(lineData.Id))
        //                     {
        //                         isValid = false;
        //                     
        //                         Console.WriteLine($"Table \"{tableData.Name}\" has invalid name for id \"{lineData.Id}\": " +
        //                                           $"Row [{lineData.Row + 1}], " +
        //                                           $"Col [{IndexToColumn(tableData.StartCol)}]");
        //                     }
        //                 }
        //             }
        //         }
        //     }
        //
        //     return isValid;
        // }

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
            
            if (value.StartsWith('$'))
            {
                result = result.TrimStart('$').Pascalize();
            }
            
            return RemoveWhitespaces(result);
        }
        
        private static string ExtractFieldName(string value)
        {
            string result = value.Pascalize();
            return RemoveWhitespaces(result);
        }

        private static string RemoveWhitespaces(string value)
        {
            string result = new string(value.Where(c => !char.IsWhiteSpace(c)).ToArray());
            return result;
        }
    }
}
