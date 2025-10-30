namespace WebApplication1.Models
{
    public enum FieldType
    {
        Int,
        Long,
        Double,
        Bool,
        String,
        Reference
    }

    public class FieldSchema
    {
        public required string Name { get; set; }
        public required FieldType Type { get; set; }

        // если Type == Reference, сюда передается полное имя типа, например:
        // "WebApplication1.Models.Generated.Employee"
        public string? ReferenceType { get; set; }
    }
}