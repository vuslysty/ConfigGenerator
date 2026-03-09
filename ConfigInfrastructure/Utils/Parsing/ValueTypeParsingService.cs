using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace ConfigGenerator.ConfigInfrastructure.Utils;

public static class ValueTypeParsingService
{
    private static readonly Regex ArrayTypeRegex = new(
        @"^(.+?)\[(.*)\]$",
        RegexOptions.Compiled);

    public static bool TryParseArrayType(string typeName, out string? specialDelimiter, out string? cleanTypeName)
    {
        specialDelimiter = null;
        cleanTypeName = null;

        if (string.IsNullOrWhiteSpace(typeName))
        {
            return false;
        }

        string normalizedTypeName = typeName.Trim();
        Match match = ArrayTypeRegex.Match(normalizedTypeName);

        if (!match.Success)
        {
            return false;
        }

        cleanTypeName = match.Groups[1].Value.Trim();
        string bracketContent = match.Groups[2].Value;

        if (string.IsNullOrWhiteSpace(bracketContent))
        {
            return true;
        }

        specialDelimiter = bracketContent;
        return true;
    }

    public static string[] TokenizeArrayValue(string input, string? delimiter = null)
    {
        if (string.IsNullOrEmpty(input))
        {
            return Array.Empty<string>();
        }

        if (string.IsNullOrEmpty(delimiter))
        {
            return [input.Trim()];
        }

        List<string> tokens = new();
        int position = 0;
        bool inQuotes = false;
        StringBuilder currentToken = new();

        while (position < input.Length)
        {
            char currentChar = input[position];

            if (currentChar == '\\' && position + 1 < input.Length)
            {
                AppendEscapedCharacter(input[position + 1], currentToken);
                position += 2;
                continue;
            }

            if (currentChar == '"')
            {
                inQuotes = !inQuotes;
                currentToken.Append(currentChar);
                position++;
                continue;
            }

            if (!inQuotes && IsDelimiterAtPosition(input, delimiter, position))
            {
                AddCurrentToken(tokens, currentToken);
                position += delimiter.Length;
                continue;
            }

            currentToken.Append(currentChar);
            position++;
        }

        AddCurrentToken(tokens, currentToken);

        for (int i = 0; i < tokens.Count; i++)
        {
            tokens[i] = ProcessToken(tokens[i]);
        }

        return [.. tokens];
    }

    private static bool IsDelimiterAtPosition(string input, string delimiter, int position)
    {
        if (position + delimiter.Length > input.Length)
        {
            return false;
        }

        return string.CompareOrdinal(input, position, delimiter, 0, delimiter.Length) == 0;
    }

    private static void AddCurrentToken(List<string> tokens, StringBuilder currentToken)
    {
        if (currentToken.Length == 0)
        {
            return;
        }

        tokens.Add(currentToken.ToString().Trim());
        currentToken.Clear();
    }

    private static void AppendEscapedCharacter(char escapedCharacter, StringBuilder target)
    {
        switch (escapedCharacter)
        {
            case '\\': target.Append('\\'); break;
            case '"': target.Append('"'); break;
            case 'n': target.Append('\n'); break;
            case 'r': target.Append('\r'); break;
            case 't': target.Append('\t'); break;
            case 'b': target.Append('\b'); break;
            case 'f': target.Append('\f'); break;
            case '0': target.Append('\0'); break;
            default: target.Append(escapedCharacter); break;
        }
    }

    private static string ProcessToken(string token)
    {
        if (token.Length >= 2 && token.StartsWith("\"") && token.EndsWith("\""))
        {
            return token[1..^1];
        }

        return token;
    }
}
