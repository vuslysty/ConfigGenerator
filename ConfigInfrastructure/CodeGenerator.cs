using System.Collections.Generic;
using System.Linq;
using ConfigGenerator.ConfigInfrastructure.TypeDesctiptors;
using Humanizer;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace ConfigGenerator.ConfigInfrastructure;

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
            var property = CreateProperty(typeDescriptor.RealTypeName, dataItem.Id, dataItem.Comment);
            properties.Add(property);
        }

        return valueTableClass.AddMembers(properties.ToArray());
    }
    
    private static ClassDeclarationSyntax GenerateDatabaseTableClass(DatabaseTableData databaseTableData, AvailableTypes availableTypes)
    {
        var idTypeDescriptor = availableTypes.GetTypeDescriptor(databaseTableData.IdType);
        
        var itemClass = SyntaxFactory.ClassDeclaration("Item")
            .AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword))
            .AddBaseListTypes(SyntaxFactory.SimpleBaseType(SyntaxFactory.ParseTypeName(
                $"ConfigTableItem<{databaseTableData.IdType}>"))
            );
        
        var properties = new List<MemberDeclarationSyntax>();
        
        foreach (var fieldDescriptor in databaseTableData.FieldDescriptors)
        {
            var fieldTypeDescriptor = availableTypes.GetTypeDescriptor(fieldDescriptor.TypeName);
            var property = CreateProperty(fieldTypeDescriptor.RealTypeName, fieldDescriptor.FieldName, fieldDescriptor.Comment);
            
            properties.Add(property);
        }

        itemClass = itemClass.AddMembers(properties.ToArray());
        
        var databaseTableClass = SyntaxFactory.ClassDeclaration(databaseTableData.Name)
            .AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword))
            .AddBaseListTypes(SyntaxFactory.SimpleBaseType(SyntaxFactory.ParseTypeName(
                $"DatabaseConfigTable<{databaseTableData.Name}.Item, {databaseTableData.IdType}>")))
            .AddMembers(itemClass);
        
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
            GenerateInitMethod("string", "jsonData", className),
            GenerateInitMethod("List<TableData>", "tables", className));
        
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
    
    public static MethodDeclarationSyntax GenerateInitMethod(string parameterType, string parameterName, string configsClassName)
    {
        return SyntaxFactory.MethodDeclaration(
                SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.VoidKeyword)), 
                "Init")
            .AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword), SyntaxFactory.Token(SyntaxKind.StaticKeyword))
            .AddParameterListParameters(
                SyntaxFactory.Parameter(SyntaxFactory.Identifier(parameterName))
                    .WithType(SyntaxFactory.ParseTypeName(parameterType))
            )
            .WithBody(SyntaxFactory.Block(
                GenerateSingletonInitialization(configsClassName), 
                SyntaxFactory.ExpressionStatement(
                    SyntaxFactory.InvocationExpression(
                        SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                            SyntaxFactory.IdentifierName("_instance"),
                            SyntaxFactory.IdentifierName("Initialize")
                        ),
                        SyntaxFactory.ArgumentList(
                            SyntaxFactory.SingletonSeparatedList(
                                SyntaxFactory.Argument(SyntaxFactory.IdentifierName(parameterName))
                            )
                        )
                    )
                )
            ));
    }

    private static PropertyDeclarationSyntax CreateProperty(string type, string name, string comment)
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