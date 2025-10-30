using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System.Reflection;

namespace WebApplication1.Services
{
    public class DynamicCompilerService
    {
        public Type CompileAndLoad(string code, string fullTypeName)
        {
            var syntaxTree = CSharpSyntaxTree.ParseText(code);

            var references = AppDomain.CurrentDomain
                .GetAssemblies()
                .Where(a => !a.IsDynamic && !string.IsNullOrWhiteSpace(a.Location))
                .Select(a => MetadataReference.CreateFromFile(a.Location));

            var compilation = CSharpCompilation.Create(
                assemblyName: $"Generated_{Guid.NewGuid()}",
                syntaxTrees: new[] { syntaxTree },
                references: references,
                options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
            );

            using var ms = new MemoryStream();
            var result = compilation.Emit(ms);

            if (!result.Success)
            {
                var errors = string.Join("\n", result.Diagnostics
                    .Where(d => d.Severity == DiagnosticSeverity.Error)
                    .Select(d => d.ToString()));

                throw new Exception($"Compilation failed:\n{errors}");
            }

            ms.Seek(0, SeekOrigin.Begin);
            var assembly = Assembly.Load(ms.ToArray());
            var type = assembly.GetType(fullTypeName);

            if (type == null)
                throw new Exception($"Type '{fullTypeName}' not found in generated assembly.");

            return type;
        }
    }
}