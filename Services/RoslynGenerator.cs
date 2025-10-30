using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;
using WebApplication1.Models;

namespace WebApplication1.Services
{
    public class RoslynGenerator
    {
        public string GenerateClassCode(string className)
        {
            var usingSystem = UsingDirective(ParseName("System"));

            var namespaceDeclaration = NamespaceDeclaration(ParseName("WebApplication1.Models.Generated"))
                .NormalizeWhitespace();

            var idProperty = PropertyDeclaration(ParseTypeName("Guid"), Identifier("Id"))
                .AddModifiers(Token(SyntaxKind.PublicKeyword))
                .AddAccessorListAccessors(
                    AccessorDeclaration(SyntaxKind.GetAccessorDeclaration)
                        .WithSemicolonToken(Token(SyntaxKind.SemicolonToken)),
                    AccessorDeclaration(SyntaxKind.SetAccessorDeclaration)
                        .WithSemicolonToken(Token(SyntaxKind.SemicolonToken))
                );

            var nameProperty = PropertyDeclaration(ParseTypeName("string"), Identifier("Name"))
                .AddModifiers(
                    Token(SyntaxKind.PublicKeyword),
                    Token(SyntaxKind.RequiredKeyword)
                )
                .AddAccessorListAccessors(
                    AccessorDeclaration(SyntaxKind.GetAccessorDeclaration)
                        .WithSemicolonToken(Token(SyntaxKind.SemicolonToken)),
                    AccessorDeclaration(SyntaxKind.SetAccessorDeclaration)
                        .WithSemicolonToken(Token(SyntaxKind.SemicolonToken))
                );

            var classDeclaration = ClassDeclaration(className)
                .AddModifiers(Token(SyntaxKind.PublicKeyword))
                .AddMembers(idProperty, nameProperty);

            var compilationUnit = CompilationUnit()
                .AddUsings(usingSystem)
                .AddMembers(namespaceDeclaration.AddMembers(classDeclaration))
                .NormalizeWhitespace();

            return compilationUnit.ToFullString();
        }

        public async Task SaveToFileAsync(string code, string className)
        {
            var dir = Path.Combine(Directory.GetCurrentDirectory(), "Models", "Generated");
            Directory.CreateDirectory(dir);

            var filePath = Path.Combine(dir, $"{className}.cs");
            await File.WriteAllTextAsync(filePath, code, System.Text.Encoding.UTF8);
        }
        
        public async Task<string> AddFieldAsync(string className, FieldSchema field)
        {
            var filePath = Path.Combine(Directory.GetCurrentDirectory(), "Models", "Generated", $"{className}.cs");

            if (!File.Exists(filePath))
                throw new FileNotFoundException($"File for class {className} not found at {filePath}");

            var code = await File.ReadAllTextAsync(filePath);
            var tree = CSharpSyntaxTree.ParseText(code);
            var root = (CompilationUnitSyntax)tree.GetRoot();

            var classNode = root.DescendantNodes()
                .OfType<ClassDeclarationSyntax>()
                .FirstOrDefault(c => c.Identifier.Text == className)
                ?? throw new Exception($"Class {className} not found in file.");

            // Определяем пространство имён текущего класса
            var currentNamespace = root.DescendantNodes()
                .OfType<NamespaceDeclarationSyntax>()
                .FirstOrDefault()?.Name.ToString();

            // Проверяем, нет ли уже такого свойства
            if (classNode.Members.OfType<PropertyDeclarationSyntax>().Any(p => p.Identifier.Text == field.Name))
                throw new Exception($"Property '{field.Name}' already exists in class '{className}'.");

            // Определяем тип свойства
            string typeName = field.Type switch
            {
                FieldType.Int => "int",
                FieldType.Long => "long",
                FieldType.Double => "double",
                FieldType.Bool => "bool",
                FieldType.String => "string",
                FieldType.Reference => ResolveReferenceTypeShortName(ref root, field.ReferenceType, currentNamespace),
                _ => "object"
            };

            var property = PropertyDeclaration(ParseTypeName(typeName), Identifier(field.Name))
                .AddModifiers(Token(SyntaxKind.PublicKeyword))
                .AddAccessorListAccessors(
                    AccessorDeclaration(SyntaxKind.GetAccessorDeclaration)
                        .WithSemicolonToken(Token(SyntaxKind.SemicolonToken)),
                    AccessorDeclaration(SyntaxKind.SetAccessorDeclaration)
                        .WithSemicolonToken(Token(SyntaxKind.SemicolonToken))
                );

            classNode = classNode.AddMembers(property);

            root = root.ReplaceNode(
                root.DescendantNodes().OfType<ClassDeclarationSyntax>().First(c => c.Identifier.Text == className),
                classNode
            );

            var formattedCode = root.NormalizeWhitespace().ToFullString();
            await File.WriteAllTextAsync(filePath, formattedCode);

            return filePath;
        }
        
        private static string ResolveReferenceTypeShortName(ref CompilationUnitSyntax root, string? typeName, string? currentNamespace)
        {
            if (string.IsNullOrWhiteSpace(typeName))
                throw new Exception("ReferenceType must be provided for Reference fields.");

            var type = AppDomain.CurrentDomain
                .GetAssemblies()
                .SelectMany(a => a.GetTypes())
                .LastOrDefault(t => t.FullName == typeName);
            if (type == null)
                throw new Exception($"Type '{typeName}' not found. Ensure it's in a loaded assembly.");

            var ns = type.Namespace;

            // Добавляем using, если namespace другой
            if (!string.IsNullOrWhiteSpace(ns) && ns != currentNamespace)
            {
                bool hasUsing = root.Usings.Any(u => u.Name.ToString() == ns);
                if (!hasUsing)
                {
                    root = root.AddUsings(UsingDirective(ParseName(ns)));
                }
            }

            // Возвращаем только короткое имя типа
            return type.Name;
        }
    }
}