using System.Reflection;
using Microsoft.AspNetCore.Mvc;
using WebApplication1.Models;
using WebApplication1.Services;
using WebApplication1.Data;

namespace WebApplication1.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DictionaryController : ControllerBase
    {
        private readonly RoslynGenerator _generator;
        private readonly DynamicCompilerService _compiler;
        private readonly AppDbContext _context;

        public DictionaryController(RoslynGenerator generator, DynamicCompilerService compiler, AppDbContext context)
        {
            _generator = generator;
            _compiler = compiler;
            _context = context;
        }

        /// <summary>
        /// Создаёт новый справочник (генерирует класс через Roslyn и загружает его в память)
        /// </summary>
        [HttpPost("generate")]
        public async Task<IActionResult> GenerateDictionary([FromBody] DictionarySchema schema)
        {
            if (string.IsNullOrWhiteSpace(schema.Name))
                return BadRequest("Name is required.");

            var className = schema.Name;
            var fullTypeName = $"WebApplication1.Models.Generated.{className}";

            var code = _generator.GenerateClassCode(className);
            await _generator.SaveToFileAsync(code, className);

            try
            {
                var type = _compiler.CompileAndLoad(code, fullTypeName);
                DynamicTypeRegistry.Register(type);

                return Ok(new
                {
                    Message = $"Class {className} generated and loaded successfully.",
                    TypeName = type.FullName,
                    Path = $"Models/Generated/{className}.cs"
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Error = ex.Message });
            }
        }

        /// <summary>
        /// Добавляет новое поле в существующий класс
        /// </summary>
        [HttpPost("{className}/add-field")]
        public async Task<IActionResult> AddField(string className, [FromBody] FieldSchema field)
        {
            try
            {
                // 1. Добавляем поле в существующий файл
                var filePath = await _generator.AddFieldAsync(className, field);

                // 2. Перечитываем и компилируем обновлённый код
                var code = await System.IO.File.ReadAllTextAsync(filePath);
                var fullTypeName = $"WebApplication1.Models.Generated.{className}";
                var type = _compiler.CompileAndLoad(code, fullTypeName);

                // 3. Обновляем реестр типов
                DynamicTypeRegistry.Register(type);

                return Ok(new
                {
                    Message = $"Field '{field.Name}' added to class '{className}' successfully.",
                    TypeName = type.FullName,
                    Field = field.Name,
                    Path = filePath
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Error = ex.Message });
            }
        }
        
        /// <summary>
        /// Возвращает все загруженные в память типы — как статические, так и динамические.
        /// </summary>
        [HttpGet("types")]
        public IActionResult GetAllTypes()
        {
            // 1️⃣ Все типы из всех загруженных сборок (включая WebApplication1.dll и динамические)
            var assemblyTypes = AppDomain.CurrentDomain
                .GetAssemblies()
                .SelectMany(a =>
                {
                    try
                    {
                        return a.GetTypes();
                    }
                    catch (ReflectionTypeLoadException ex)
                    {
                        // некоторые сборки могут не загрузиться полностью (System.Private.CoreLib и т.п.)
                        return ex.Types.Where(t => t != null)!;
                    }
                })
                .Where(t => t is { IsClass: true, Namespace: "WebApplication1.Models.Generated" })
                .ToList();

            // 2️⃣ Типы, зарегистрированные динамически вручную
            var dynamicTypes = DynamicTypeRegistry.GetAll().ToList();

            // 3️⃣ Объединяем и убираем дубликаты по FullName
            var allTypes = assemblyTypes
                .Concat(dynamicTypes)
                .GroupBy(t => t.FullName)
                .Select(g => g.Last()) // ← берём последнюю (самую свежую) версию
                .OrderBy(t => t.Name)
                .Select(t => new
                {
                    t.Name,
                    t.FullName,
                    Source = dynamicTypes.Contains(t) ? "Dynamic" : "Assembly"
                })
                .ToList();

            return Ok(allTypes);
        }
        
        /// <summary>
        /// Возвращает только динамически загруженные типы из памяти
        /// </summary>
        [HttpGet("dynamic-types")]
        public IActionResult GetDynamicTypesOnly()
        {
            var dynamicTypes = DynamicTypeRegistry
                .GetAll()
                .Select(t => new
                {
                    t.Name,
                    t.FullName,
                    Assembly = t.Assembly.GetName().Name
                })
                .OrderBy(t => t.Name)
                .ToList();

            return Ok(dynamicTypes);
        }
    }
}