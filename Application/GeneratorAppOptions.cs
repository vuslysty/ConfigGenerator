using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace ConfigGenerator.Application;

public sealed class GeneratorAppOptions
{
    public string ConfigClassName { get; set; } = "MyConfig";
    public string ConfigNamespace { get; set; } = "TestNamespace";
    public string SpreadsheetId { get; set; } = "1JphtDv8GUoyqib2y1r_FkiF6JdlrCRg_GIxpWv7v-aQ";
    public string CredentialsFile { get; set; } = "credentials.json";
    public string? ProjectDirectory { get; set; }
    public string? GeneratedFolder { get; set; }
    public bool PrintValidationIssues { get; set; }
    public bool PrintGenerationMessages { get; set; }
    public string Mode { get; set; } = "generate";
    public string? JsonInputFile { get; set; }

    public GeneratorRunMode ResolveRunMode()
    {
        if (string.Equals(Mode, "parse", StringComparison.OrdinalIgnoreCase))
        {
            return GeneratorRunMode.Parse;
        }

        if (string.Equals(Mode, "validate", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(Mode, "validate-json", StringComparison.OrdinalIgnoreCase))
        {
            return GeneratorRunMode.Validate;
        }

        if (string.Equals(Mode, "generate-code", StringComparison.OrdinalIgnoreCase))
        {
            return GeneratorRunMode.GenerateCode;
        }

        if (string.Equals(Mode, "generate-json", StringComparison.OrdinalIgnoreCase))
        {
            return GeneratorRunMode.GenerateJson;
        }

        return GeneratorRunMode.GenerateArtifacts;
    }

    public string ResolveProjectDirectory()
    {
        if (!string.IsNullOrWhiteSpace(ProjectDirectory))
        {
            return ProjectDirectory;
        }

        return Directory.GetParent(AppDomain.CurrentDomain.BaseDirectory)!.Parent!.Parent!.Parent!.FullName;
    }

    public string ResolveGeneratedFolder()
    {
        if (!string.IsNullOrWhiteSpace(GeneratedFolder))
        {
            return GeneratedFolder;
        }

        return Path.Combine(ResolveProjectDirectory(), "Generated");
    }

    public static GeneratorAppOptions Load(string[] args)
    {
        Dictionary<string, string> cli = ParseCli(args);
        string configPath = cli.TryGetValue("config", out string? explicitConfigPath)
            ? explicitConfigPath
            : "generator.settings.json";

        GeneratorAppOptions options = LoadFromFile(configPath) ?? new GeneratorAppOptions();

        // Backward-compatible env support (lower priority than explicit config file).
        ApplyEnvironmentFallbacks(options);

        // CLI override has the highest priority.
        ApplyCliOverrides(options, cli);
        return options;
    }

    private static GeneratorAppOptions? LoadFromFile(string configPath)
    {
        if (!File.Exists(configPath))
        {
            return null;
        }

        string json = File.ReadAllText(configPath);
        return JsonSerializer.Deserialize<GeneratorAppOptions>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
        });
    }

    private static void ApplyEnvironmentFallbacks(GeneratorAppOptions options)
    {
        options.ConfigClassName = FirstNonEmpty(options.ConfigClassName, Environment.GetEnvironmentVariable("CONFIG_CLASS_NAME")) ?? options.ConfigClassName;
        options.ConfigNamespace = FirstNonEmpty(options.ConfigNamespace, Environment.GetEnvironmentVariable("CONFIG_NAMESPACE")) ?? options.ConfigNamespace;
        options.SpreadsheetId = FirstNonEmpty(options.SpreadsheetId, Environment.GetEnvironmentVariable("SPREADSHEET_ID")) ?? options.SpreadsheetId;
        options.CredentialsFile = FirstNonEmpty(options.CredentialsFile, Environment.GetEnvironmentVariable("GOOGLE_CREDENTIALS_FILE")) ?? options.CredentialsFile;
        options.ProjectDirectory = FirstNonEmpty(options.ProjectDirectory, Environment.GetEnvironmentVariable("PROJECT_DIRECTORY"));
        options.GeneratedFolder = FirstNonEmpty(options.GeneratedFolder, Environment.GetEnvironmentVariable("GENERATED_FOLDER"));
        options.PrintValidationIssues = options.PrintValidationIssues || ReadBool(Environment.GetEnvironmentVariable("PRINT_VALIDATION_ISSUES"));
        options.PrintGenerationMessages = options.PrintGenerationMessages || ReadBool(Environment.GetEnvironmentVariable("PRINT_GENERATION_MESSAGES"));
        options.Mode = FirstNonEmpty(options.Mode, Environment.GetEnvironmentVariable("GENERATOR_MODE")) ?? options.Mode;
        options.JsonInputFile = FirstNonEmpty(options.JsonInputFile, Environment.GetEnvironmentVariable("INPUT_JSON_FILE"));
    }

    private static void ApplyCliOverrides(GeneratorAppOptions options, Dictionary<string, string> cli)
    {
        options.ConfigClassName = FirstNonEmpty(cli.GetValueOrDefault("config-class-name"), options.ConfigClassName) ?? options.ConfigClassName;
        options.ConfigNamespace = FirstNonEmpty(cli.GetValueOrDefault("config-namespace"), options.ConfigNamespace) ?? options.ConfigNamespace;
        options.SpreadsheetId = FirstNonEmpty(cli.GetValueOrDefault("spreadsheet-id"), options.SpreadsheetId) ?? options.SpreadsheetId;
        options.CredentialsFile = FirstNonEmpty(cli.GetValueOrDefault("google-credentials-file"), options.CredentialsFile) ?? options.CredentialsFile;
        options.ProjectDirectory = FirstNonEmpty(cli.GetValueOrDefault("project-directory"), options.ProjectDirectory);
        options.GeneratedFolder = FirstNonEmpty(cli.GetValueOrDefault("generated-folder"), options.GeneratedFolder);
        options.PrintValidationIssues = options.PrintValidationIssues || ReadBool(cli.GetValueOrDefault("print-validation-issues"));
        options.PrintGenerationMessages = options.PrintGenerationMessages || ReadBool(cli.GetValueOrDefault("print-generation-messages"));
        options.Mode = FirstNonEmpty(cli.GetValueOrDefault("mode"), options.Mode) ?? options.Mode;
        options.JsonInputFile = FirstNonEmpty(cli.GetValueOrDefault("json-input-file"), options.JsonInputFile);
    }

    private static Dictionary<string, string> ParseCli(string[] args)
    {
        Dictionary<string, string> values = new(StringComparer.OrdinalIgnoreCase);

        foreach (string arg in args)
        {
            if (!arg.StartsWith("--", StringComparison.Ordinal))
            {
                continue;
            }

            string token = arg[2..];
            int separatorIndex = token.IndexOf('=');

            if (separatorIndex <= 0)
            {
                values[token] = "true";
                continue;
            }

            string key = token[..separatorIndex];
            string value = token[(separatorIndex + 1)..];
            values[key] = value;
        }

        return values;
    }

    private static string? FirstNonEmpty(string? first, string? second)
    {
        if (!string.IsNullOrWhiteSpace(first))
        {
            return first;
        }

        return string.IsNullOrWhiteSpace(second) ? null : second;
    }

    private static bool ReadBool(string? value)
    {
        return string.Equals(value, "true", StringComparison.OrdinalIgnoreCase);
    }
}
