using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace WebApplication1
{
    public class SchemaFilter : ISchemaFilter
    {
        public void Apply(OpenApiSchema schema, SchemaFilterContext context)
        {
            if (schema?.Properties == null)
                return;

            // 🔹 1. Убираем свойства, помеченные как readOnly
            var propsToRemove = schema
                .Properties
                .Where(kv => kv.Value.ReadOnly)
                .Select(kv => kv.Key)
                .ToList();

            foreach (var propName in propsToRemove)
            {
                schema.Properties.Remove(propName);

                if (schema.Required != null && schema.Required.Contains(propName))
                    schema.Required.Remove(propName);
            }

            // 🔹 2. Добавляем пример только для DictionarySchema
            if (context.Type.FullName == "WebApplication1.Models.DictionarySchema")
            {
                if (schema.Properties.ContainsKey("name"))
                {
                    schema.Properties["name"].Example = new OpenApiString("test");
                }
            }
        }
    }
}