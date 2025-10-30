namespace WebApplication1.Services
{
    public static class DynamicTypeRegistry
    {
        private static readonly List<Type> _types = new();

        public static void Register(Type type)
        {
            if (!_types.Contains(type))
                _types.Add(type);
        }

        public static IEnumerable<Type> GetAll() => _types;
    }
}