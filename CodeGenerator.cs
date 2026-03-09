using System;
using System.Collections.Generic;
using System.Linq;
using CaseConverter;
using ConfigGenerator.ConfigInfrastructure;
using ConfigGenerator.ConfigInfrastructure.Data;
using ConfigGenerator.ConfigInfrastructure.TypeDesctiptors;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace ConfigGenerator;

public static class CodeGenerator
{
    private static readonly IReadOnlyDictionary<Type, Func<TableData, AvailableTypes, ClassDeclarationSyntax>> TableClassGenerators =
        new Dictionary<Type, Func<TableData, AvailableTypes, ClassDeclarationSyntax>>
        {
            [typeof(ValueTableData)] = static (table, availableTypes) =>
                GenerateValueTableClass((ValueTableData)table, availableTypes),
            [typeof(DatabaseTableData)] = static (table, availableTypes) =>
                GenerateDatabaseTableClass((DatabaseTableData)table, availableTypes),
            [typeof(ConstantTableData)] = static (table, _) =>
                GenerateConstantTableClass((ConstantTableData)table),
        };

    public static string GenerateConfigClasses(List<TableData> tables, string className, string namespaceName)
    {
        return GenerateConfigClasses(tables, className, namespaceName, new Application.TypeRegistryFactory());
    }

    public static string GenerateConfigClasses(List<TableData> tables, string className, string namespaceName, Application.ITypeRegistryFactory typeRegistryFactory)
    {
        AvailableTypes availableTypes = typeRegistryFactory.CreateForTables(tables);

        List<ClassDeclarationSyntax> classes = new()
        {
            GenerateConfigClass(tables, className),
        };

        foreach (TableData table in tables)
        {
            classes.Add(GenerateTableClass(table, availableTypes));
        }

        var namespaceDecl = SyntaxFactory
            .NamespaceDeclaration(SyntaxFactory.ParseName(namespaceName))
            .AddMembers(classes.ToArray());

        var syntaxTree = SyntaxFactory.CompilationUnit()
            .AddUsings(
                SyntaxFactory.UsingDirective(SyntaxFactory.ParseName("ConfigGenerator.ConfigInfrastructure")),
                SyntaxFactory.UsingDirective(SyntaxFactory.ParseName("ConfigGenerator.ConfigInfrastructure.Data")),
                SyntaxFactory.UsingDirective(SyntaxFactory.ParseName("System")),
                SyntaxFactory.UsingDirective(SyntaxFactory.ParseName("System.Collections.Generic")))
            .AddMembers(namespaceDecl);

        return syntaxTree.NormalizeWhitespace().ToFullString();
    }

    private static ClassDeclarationSyntax GenerateTableClass(TableData tableData, AvailableTypes availableTypes)
    {
        if (TableClassGenerators.TryGetValue(tableData.GetType(), out Func<TableData, AvailableTypes, ClassDeclarationSyntax>? generator))
        {
            return generator(tableData, availableTypes);
        }

        throw new InvalidOperationException($"Unsupported table type for code generation: {tableData.GetType().Name}");
    }

    private static ClassDeclarationSyntax GenerateValueTableClass(ValueTableData valueTableData, AvailableTypes availableTypes)
    {
        var valueTableClass = SyntaxFactory.ClassDeclaration(valueTableData.Name)
            .AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword))
            .AddBaseListTypes(SyntaxFactory.SimpleBaseType(SyntaxFactory.ParseTypeName("ValueConfigTable")));

        List<MemberDeclarationSyntax> properties = new();

        foreach (ValueTableDataItem dataItem in valueTableData.Items)
        {
            var typeDescriptor = availableTypes.GetTypeDescriptor(dataItem.Type);

            string typeName = typeDescriptor.RealTypeName;

            if (dataItem.ArrayType.IsArray())
            {
                typeName = $"{typeName}[]";
            }

            properties.Add(CreateProperty(typeName, dataItem.Id, dataItem.Comment));
        }

        return valueTableClass.AddMembers(properties.ToArray());
    }

    private static ClassDeclarationSyntax GenerateConstantTableClass(ConstantTableData constantTableData)
    {
        var valueTableClass = SyntaxFactory.ClassDeclaration(constantTableData.Name)
            .AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword));

        List<MemberDeclarationSyntax> constants = new();

        foreach (ConstantTableDataItem dataItem in constantTableData.Items)
        {
            var constantField = SyntaxFactory.FieldDeclaration(
                    SyntaxFactory.VariableDeclaration(SyntaxFactory.ParseTypeName("int"))
                        .AddVariables(SyntaxFactory.VariableDeclarator(dataItem.Name)
                            .WithInitializer(SyntaxFactory.EqualsValueClause(
                                    SyntaxFactory.LiteralExpression(
                                        SyntaxKind.NumericLiteralExpression,
                                        SyntaxFactory.Literal(dataItem.Value))))))
                    .AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword), SyntaxFactory.Token(SyntaxKind.ConstKeyword));

            if (!string.IsNullOrWhiteSpace(dataItem.Comment))
            {
                constantField = constantField.WithLeadingTrivia(CreateXmlComment(dataItem.Comment));
            }

            constants.Add(constantField);
        }

        return valueTableClass.AddMembers(constants.ToArray());
    }

    private static ClassDeclarationSyntax GenerateClass(FieldNode fieldNode, AvailableTypes availableTypes)
    {
        var newClass = SyntaxFactory.ClassDeclaration(fieldNode.BaseType)
            .AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword));

        GenerateFieldMembers(fieldNode.Children, availableTypes, out List<MemberDeclarationSyntax> properties, out List<MemberDeclarationSyntax> innerClasses);

        return newClass
            .AddMembers(innerClasses.ToArray())
            .AddMembers(properties.ToArray());
    }

    private static void GenerateFieldMembers(
        IEnumerable<FieldNode> fieldNodes,
        AvailableTypes availableTypes,
        out List<MemberDeclarationSyntax> properties,
        out List<MemberDeclarationSyntax> innerClasses)
    {
        properties = new List<MemberDeclarationSyntax>();
        innerClasses = new List<MemberDeclarationSyntax>();

        foreach (FieldNode child in fieldNodes)
        {
            if (child.Children.Count > 0)
            {
                properties.Add(CreateProperty($"List<{child.BaseType}>", child.Name));
                innerClasses.Add(GenerateClass(child, availableTypes));
                continue;
            }

            properties.Add(CreateFieldProperty(child, availableTypes));
        }
    }

    private static PropertyDeclarationSyntax CreateFieldProperty(FieldNode fieldNode, AvailableTypes availableTypes)
    {
        var fieldTypeDescriptor = availableTypes.GetTypeDescriptor(fieldNode.BaseType);

        string typeName = fieldTypeDescriptor.RealTypeName;

        if (fieldNode.ArrayType.IsArray())
        {
            typeName = $"{typeName}[]";
        }

        return CreateProperty(typeName, fieldNode.Name, fieldNode.Comment);
    }

    private static ClassDeclarationSyntax GenerateDatabaseTableClass(DatabaseTableData databaseTableData, AvailableTypes availableTypes)
    {
        GenerateFieldMembers(databaseTableData.RootFieldNode.Children.Skip(1), availableTypes, out List<MemberDeclarationSyntax> properties, out List<MemberDeclarationSyntax> innerClasses);

        string idType = databaseTableData.IdType;
        TypeDescriptor? idTypeDescription = availableTypes.GetTypeDescriptor(idType);

        ClassDeclarationSyntax itemMainClass = SyntaxFactory.ClassDeclaration("Item")
            .AddBaseListTypes(SyntaxFactory.SimpleBaseType(SyntaxFactory.ParseTypeName($"ConfigTableItem<{idTypeDescription.RealTypeName}>")))
            .AddMembers(properties.ToArray());

        ClassDeclarationSyntax? itemPartialClass = null;

        if (innerClasses.Count > 0)
        {
            itemMainClass = itemMainClass.AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword),
                SyntaxFactory.Token(SyntaxKind.PartialKeyword));

            itemPartialClass = SyntaxFactory.ClassDeclaration("Item")
                .AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword),
                    SyntaxFactory.Token(SyntaxKind.PartialKeyword))
                .AddMembers(innerClasses.ToArray());
        }
        else
        {
            itemMainClass = itemMainClass.AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword));
        }

        var databaseTableClass = SyntaxFactory.ClassDeclaration(databaseTableData.Name)
            .AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword))
            .AddBaseListTypes(SyntaxFactory.SimpleBaseType(SyntaxFactory.ParseTypeName(
                $"DatabaseConfigTable<{databaseTableData.Name}.Item, {idTypeDescription.RealTypeName}>")))
            .AddMembers(itemMainClass);

        if (itemPartialClass != null)
        {
            databaseTableClass = databaseTableClass.AddMembers(itemPartialClass);
        }

        return databaseTableClass;
    }

    private static ClassDeclarationSyntax GenerateConfigClass(List<TableData> tables, string className)
    {
        var configClass = SyntaxFactory.ClassDeclaration(className)
            .AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword))
            .AddBaseListTypes(SyntaxFactory.SimpleBaseType(SyntaxFactory.ParseTypeName("ConfigsBase")));

        var instanceField = SyntaxFactory.FieldDeclaration(
            SyntaxFactory.VariableDeclaration(SyntaxFactory.ParseTypeName(className))
                .AddVariables(SyntaxFactory.VariableDeclarator("_instance")))
            .AddModifiers(SyntaxFactory.Token(SyntaxKind.PrivateKeyword), SyntaxFactory.Token(SyntaxKind.StaticKeyword));

        configClass = configClass.AddMembers(instanceField);

        foreach (TableData table in tables.Where(ShouldGenerateConfigAccessor))
        {
            string fieldName = $"_{table.Name.ToCamelCase()}";

            TypeSyntax tableType = SyntaxFactory.ParseTypeName(table.Name);

            var fieldDeclaration = SyntaxFactory.VariableDeclaration(tableType)
                .AddVariables(SyntaxFactory.VariableDeclarator(fieldName)
                    .WithInitializer(SyntaxFactory.EqualsValueClause(
                        SyntaxFactory.ObjectCreationExpression(tableType)
                            .WithArgumentList(SyntaxFactory.ArgumentList()))));

            var field = SyntaxFactory.FieldDeclaration(fieldDeclaration)
                .AddModifiers(SyntaxFactory.Token(SyntaxKind.PrivateKeyword), SyntaxFactory.Token(SyntaxKind.ReadOnlyKeyword));

            var property = SyntaxFactory.PropertyDeclaration(tableType, table.Name)
                .AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword), SyntaxFactory.Token(SyntaxKind.StaticKeyword))
                .WithExpressionBody(SyntaxFactory.ArrowExpressionClause(
                    SyntaxFactory.MemberAccessExpression(
                        SyntaxKind.SimpleMemberAccessExpression,
                        SyntaxFactory.InvocationExpression(SyntaxFactory.IdentifierName("GetConfigs")),
                        SyntaxFactory.IdentifierName(fieldName))))
                .WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken));

            configClass = configClass.AddMembers(field, property);
        }

        return configClass.AddMembers(
            GenerateGetConfigsMethod(className),
            GenerateInitMethod([("string", "jsonData"), ("ITableDataSerializer", "tableDataSerializer")], className),
            GenerateInitMethod([("List<TableData>", "tables")], className));
    }

    private static bool ShouldGenerateConfigAccessor(TableData tableData)
    {
        return tableData is not ConstantTableData;
    }

    private static IfStatementSyntax GenerateSingletonInitialization(string className)
    {
        return SyntaxFactory.IfStatement(
            SyntaxFactory.BinaryExpression(SyntaxKind.EqualsExpression,
                SyntaxFactory.IdentifierName("_instance"),
                SyntaxFactory.LiteralExpression(SyntaxKind.NullLiteralExpression)
            ),
            SyntaxFactory.Block(
                SyntaxFactory.ExpressionStatement(
                    SyntaxFactory.AssignmentExpression(SyntaxKind.SimpleAssignmentExpression,
                        SyntaxFactory.IdentifierName("_instance"),
                        SyntaxFactory.ObjectCreationExpression(SyntaxFactory.IdentifierName(className))
                            .WithArgumentList(SyntaxFactory.ArgumentList())
                    )
                )
            )
        );
    }

    private static MethodDeclarationSyntax GenerateGetConfigsMethod(string configTypeName)
    {
        return SyntaxFactory.MethodDeclaration(SyntaxFactory.IdentifierName(configTypeName), "GetConfigs")
            .AddModifiers(SyntaxFactory.Token(SyntaxKind.PrivateKeyword), SyntaxFactory.Token(SyntaxKind.StaticKeyword))
            .WithBody(SyntaxFactory.Block(
                GenerateSingletonInitialization(configTypeName),
                SyntaxFactory.IfStatement(
                    SyntaxFactory.PrefixUnaryExpression(SyntaxKind.LogicalNotExpression,
                        SyntaxFactory.IdentifierName("_instance._initialized")
                    ),
                    SyntaxFactory.Block(
                        SyntaxFactory.ExpressionStatement(
                            SyntaxFactory.InvocationExpression(
                                SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                                    SyntaxFactory.IdentifierName("Console"),
                                    SyntaxFactory.IdentifierName("WriteLine")
                                ),
                                SyntaxFactory.ArgumentList(
                                    SyntaxFactory.SingletonSeparatedList(
                                        SyntaxFactory.Argument(
                                            SyntaxFactory
                                                .InterpolatedStringExpression(
                                                    SyntaxFactory.Token(SyntaxKind.InterpolatedStringStartToken))
                                                .AddContents(
                                                    SyntaxFactory.InterpolatedStringText()
                                                        .WithTextToken(SyntaxFactory.Token(
                                                            SyntaxFactory.TriviaList(),
                                                            SyntaxKind.InterpolatedStringTextToken,
                                                            $"{configTypeName} is not initialized!",
                                                            $"{configTypeName} is not initialized!",
                                                            SyntaxFactory.TriviaList()
                                                        ))
                                                )
                                        )
                                    )
                                )
                            )
                        )
                    )
                ),
                SyntaxFactory.ReturnStatement(SyntaxFactory.IdentifierName("_instance"))
            ));
    }

    public static MethodDeclarationSyntax GenerateInitMethod(
        List<(string type, string name)> parameters, string configsClassName)
    {
        ParameterSyntax[] parameterSyntaxItems = new ParameterSyntax[parameters.Count];
        SeparatedSyntaxList<ArgumentSyntax> arguments = SyntaxFactory.SeparatedList(parameters.Select(parameter =>
            SyntaxFactory.Argument(SyntaxFactory.IdentifierName(parameter.name))).ToArray());

        for (int i = 0; i < parameters.Count; i++)
        {
            (string type, string name) parameter = parameters[i];

            parameterSyntaxItems[i] = SyntaxFactory.Parameter(SyntaxFactory.Identifier(parameter.name))
                .WithType(SyntaxFactory.ParseTypeName(parameter.type));
        }

        return SyntaxFactory.MethodDeclaration(
                SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.VoidKeyword)),
                "Init")
            .AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword), SyntaxFactory.Token(SyntaxKind.StaticKeyword))
            .AddParameterListParameters(parameterSyntaxItems)
            .WithBody(SyntaxFactory.Block(
                GenerateSingletonInitialization(configsClassName),
                SyntaxFactory.ExpressionStatement(
                    SyntaxFactory.InvocationExpression(
                        SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                            SyntaxFactory.IdentifierName("_instance"),
                            SyntaxFactory.IdentifierName("Initialize")
                        ),
                        SyntaxFactory.ArgumentList(arguments)
                    )
                )
            ));
    }

    private static PropertyDeclarationSyntax CreateProperty(string type, string name, string? comment = null)
    {
        var property = SyntaxFactory.PropertyDeclaration(SyntaxFactory.ParseTypeName(type), name)
            .AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword))
            .WithAccessorList(SyntaxFactory.AccessorList(
                SyntaxFactory.List(new[]
                {
                    SyntaxFactory.AccessorDeclaration(SyntaxKind.GetAccessorDeclaration)
                        .WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken)),

                    SyntaxFactory.AccessorDeclaration(SyntaxKind.SetAccessorDeclaration)
                        .AddModifiers(SyntaxFactory.Token(SyntaxKind.PrivateKeyword))
                        .WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken))
                })));

        if (!string.IsNullOrWhiteSpace(comment))
        {
            property = property.WithLeadingTrivia(CreateXmlComment(comment));
        }

        return property;
    }

    private static SyntaxTriviaList CreateXmlComment(string commentText)
    {
        var lines = commentText.Split('\n')
            .Select(line => "/// " + line.Trim())
            .Prepend("/// <summary>")
            .Append("/// </summary>");

        var triviaList = lines
            .SelectMany(line => new[]
            {
                SyntaxFactory.Comment(line)
            })
            .Prepend(SyntaxFactory.DisabledText("\n"))
            .ToArray();

        return SyntaxFactory.TriviaList(triviaList);
    }
}
