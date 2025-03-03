using System.Collections.Generic;
using System.Linq;
using Humanizer;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace ConfigGenerator.ConfigInfrastructure;

public static class CodeGenerator
{
    public static string GenerateConfigClasses(List<TableData> tables)
    {
        List<ClassDeclarationSyntax> classes = new List<ClassDeclarationSyntax>();
        
        classes.Add(GenerateConfigClass(tables));

        foreach (var table in tables)
        {
            if (table is ValueTableData valueTableData)
            {
                classes.Add(GenerateValueTableClass(valueTableData, tables));
            }
            else if (table is DatabaseTableData databaseTableData)
            {
                classes.Add(GenerateDatabaseTableClass(databaseTableData, tables));
            }
        }
        
        var namespaceDecl = SyntaxFactory
            .NamespaceDeclaration(SyntaxFactory.ParseName("YourNamespace"))
            .AddMembers(classes.ToArray());

        var syntaxTree = SyntaxFactory.CompilationUnit()
            .AddUsings(SyntaxFactory.UsingDirective(SyntaxFactory.ParseName("ConfigGenerator.ConfigInfrastructure")))
            .AddMembers(namespaceDecl);

        var formattedCode = syntaxTree.NormalizeWhitespace().ToFullString();
        return formattedCode;
    }

    private static ClassDeclarationSyntax GenerateValueTableClass(ValueTableData valueTableData, List<TableData> allTables)
    {
        var valueTableClass = SyntaxFactory.ClassDeclaration(valueTableData.Name)
            .AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword))
            .AddBaseListTypes(SyntaxFactory.SimpleBaseType(SyntaxFactory.ParseTypeName("ValueConfigTable")));

        List<MemberDeclarationSyntax> properties = new List<MemberDeclarationSyntax>();
        
        foreach (var dataItem in valueTableData.DataValues)
        {
            var property = CreateProperty(dataItem.Type, dataItem.Id, dataItem.Comment, allTables);
            properties.Add(property);
        }

        return valueTableClass.AddMembers(properties.ToArray());
    }
    
    private static ClassDeclarationSyntax GenerateDatabaseTableClass(DatabaseTableData databaseTableData, List<TableData> allTables)
    {
        var itemClass = SyntaxFactory.ClassDeclaration("Item")
            .AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword))
            .AddBaseListTypes(SyntaxFactory.SimpleBaseType(SyntaxFactory.ParseTypeName(
                $"IConfigTableItem<{databaseTableData.IdType}>")))
            .AddMembers(
                CreateProperty(databaseTableData.IdType, "Id", null, allTables),
                CreateProperty("int", "Index", null, allTables)
            );
        
        var properties = new List<MemberDeclarationSyntax>();
        
        foreach (var fieldDescriptor in databaseTableData.FieldDescriptors)
        {
            var property = CreateProperty(fieldDescriptor.TypeName, fieldDescriptor.FieldName, fieldDescriptor.Comment,
                allTables);
            
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

    private static ClassDeclarationSyntax GenerateConfigClass(List<TableData> tables)
    {
        var configClass = SyntaxFactory.ClassDeclaration("Configs")
            .AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword))
            .AddBaseListTypes(SyntaxFactory.SimpleBaseType(SyntaxFactory.ParseTypeName("ConfigsBase")));
        
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
                .AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword))
                .WithExpressionBody(SyntaxFactory.ArrowExpressionClause(
                    SyntaxFactory.InvocationExpression(
                            SyntaxFactory.IdentifierName("GetConfig"))
                        .WithArgumentList(SyntaxFactory.ArgumentList(
                            SyntaxFactory.SingletonSeparatedList(
                                SyntaxFactory.Argument(SyntaxFactory.IdentifierName(fieldName)))))))
                .WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken));
            
            configClass = configClass.AddMembers(field, property);
        }
        
        return configClass;
    }

    private static PropertyDeclarationSyntax CreateProperty(string type, string name, string comment,
        List<TableData> allTables)
    {
        var propertyTypeName = allTables.Exists(data => data.Name == type)
            ? $"{type}.Item"
            : type;

        var property = SyntaxFactory.PropertyDeclaration(SyntaxFactory.ParseTypeName(propertyTypeName), name)
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
            .ToArray();

        return SyntaxFactory.TriviaList(triviaList);
    }
}