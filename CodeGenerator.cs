using System.Collections.Generic;
using System.Linq;
using ConfigGenerator.ConfigInfrastructure;
using ConfigGenerator.ConfigInfrastructure.Data;
using ConfigGenerator.ConfigInfrastructure.TypeDesctiptors;
using Humanizer;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace ConfigGenerator;

public static class CodeGenerator
{
    public static string GenerateConfigClasses(List<TableData> tables, string className, string namespaceName)
    {
        AvailableTypes availableTypes = new AvailableTypes();
        availableTypes.RegisterDefaultTypes();

        foreach (var tableData in tables)
        {
            switch (tableData)
            {
                case DatabaseTableData databaseTableData:
                    availableTypes.Register(new DatabaseTableTypeDescriptor(databaseTableData));
                    break;
            }
        }
        
        List<ClassDeclarationSyntax> classes = new List<ClassDeclarationSyntax>();
        
        classes.Add(GenerateConfigClass(tables, className));

        foreach (var table in tables)
        {
            if (table is ValueTableData valueTableData)
            {
                classes.Add(GenerateValueTableClass(valueTableData, availableTypes));
            }
            else if (table is DatabaseTableData databaseTableData)
            {
                classes.Add(GenerateDatabaseTableClass(databaseTableData, availableTypes));
            }
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

        var formattedCode = syntaxTree.NormalizeWhitespace().ToFullString();
        return formattedCode;
    }

    private static ClassDeclarationSyntax GenerateValueTableClass(ValueTableData valueTableData, AvailableTypes availableTypes)
    {
        var valueTableClass = SyntaxFactory.ClassDeclaration(valueTableData.Name)
            .AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword))
            .AddBaseListTypes(SyntaxFactory.SimpleBaseType(SyntaxFactory.ParseTypeName("ValueConfigTable")));

        List<MemberDeclarationSyntax> properties = new List<MemberDeclarationSyntax>();
        
        foreach (var dataItem in valueTableData.DataValues)
        {
            var typeDescriptor = availableTypes.GetTypeDescriptor(dataItem.Type);

            string typeName = typeDescriptor.RealTypeName;

            if (dataItem.ArrayType.IsArray()) {
                typeName = $"{typeName}[]";
            }
            
            var property = CreateProperty(typeName, dataItem.Id, dataItem.Comment);
            properties.Add(property);
        }

        return valueTableClass.AddMembers(properties.ToArray());
    }

    private static ClassDeclarationSyntax GenerateClass(FieldNode fieldNode, AvailableTypes availableTypes)
    {
        var newClass = SyntaxFactory.ClassDeclaration(fieldNode.BaseType)
            .AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword));

        var properties = new List<MemberDeclarationSyntax>();
        var innerClasses = new List<MemberDeclarationSyntax>();
        
        foreach (FieldNode child in fieldNode.Children)
        {
            if (child.Children.Count > 0)
            {
                var property = CreateProperty($"List<{child.BaseType}>", child.Name);
                
                properties.Add(property);
                
                var innerClass = GenerateClass(child, availableTypes);
                innerClasses.Add(innerClass);
            }
            else
            {
                var fieldTypeDescriptor = availableTypes.GetTypeDescriptor(child.BaseType);
                
                string typeName = fieldTypeDescriptor.RealTypeName;

                if (child.ArrayType.IsArray()) {
                    typeName = $"{typeName}[]";
                }
                
                var property = CreateProperty(typeName, child.Name, child.Comment);
            
                properties.Add(property);
            }
        }
        
        newClass = newClass
            .AddMembers(innerClasses.ToArray())
            .AddMembers(properties.ToArray());
        
        return newClass;
    }
    
    private static ClassDeclarationSyntax GenerateDatabaseTableClass(DatabaseTableData databaseTableData, AvailableTypes availableTypes)
    {
        var properties = new List<MemberDeclarationSyntax>();
        var innerClasses = new List<MemberDeclarationSyntax>();

        // We started from 1 not 0 because we skip Id field
        for (var i = 1; i < databaseTableData.RootFieldNode.Children.Count; i++)
        {
            var child = databaseTableData.RootFieldNode.Children[i];
            if (child.Children.Count > 0)
            {
                var property = CreateProperty($"List<{child.BaseType}>", child.Name);

                properties.Add(property);

                var innerClass = GenerateClass(child, availableTypes);
                innerClasses.Add(innerClass);
            }
            else
            {
                var fieldTypeDescriptor = availableTypes.GetTypeDescriptor(child.BaseType);
                
                string typeName = fieldTypeDescriptor.RealTypeName;

                if (child.ArrayType.IsArray()) {
                    typeName = $"{typeName}[]";
                }
                
                var property = CreateProperty(typeName, child.Name, child.Comment);

                properties.Add(property);
            }
        }

        string idType = databaseTableData.RootFieldNode.Children[0].BaseType;
        TypeDescriptor? idTypeDescription = availableTypes.GetTypeDescriptor(idType);
        
        ClassDeclarationSyntax itemMainClass = SyntaxFactory.ClassDeclaration("Item")
            .AddBaseListTypes(SyntaxFactory.SimpleBaseType(SyntaxFactory.ParseTypeName($"ConfigTableItem<{idTypeDescription.RealTypeName}>")))
            .AddMembers(properties.ToArray());
        
        ClassDeclarationSyntax itemPartialClass = null;

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

        if (itemPartialClass != null) {
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
        
        foreach (var table in tables)
        {
            var fieldName = $"_{table.Name.Camelize()}";

            var tableType = SyntaxFactory.ParseTypeName(table.Name);
            
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
        
        configClass = configClass.AddMembers(
            GenerateGetConfigsMethod(className),
            GenerateInitMethod([("string", "jsonData"), ("ITableDataSerializer", "tableDataSerializer")], className),
            GenerateInitMethod([("List<TableData>", "tables")], className));
        
        return configClass;
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
            .SelectMany(line => new[] { 
                SyntaxFactory.Comment(line) })
            .Prepend(SyntaxFactory.DisabledText("\n"))
            .ToArray();

        return SyntaxFactory.TriviaList(triviaList);
    }
}