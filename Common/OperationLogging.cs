namespace ConfigGenerator.Common;

public static class OperationResultIdentifiers
{
    public const string Operation = "Operation";
    public const string Validator = "Validator";
    public const string Generator = "Generator";
    public const string Pipeline = "Pipeline";
}

public static class OperationSteps
{
    public static class Generation
    {
        public const string Code = "code";
        public const string Json = "json";
        public const string Artifacts = "artifacts";
    }

    public static class Validation
    {
        public const string Metadata = "validate:metadata";
        public const string Data = "validate:data";
        public const string ValueData = "validate:data:value";
        public const string DatabaseData = "validate:data:database";
        public const string ConstantData = "validate:data:constant";
    }
}
