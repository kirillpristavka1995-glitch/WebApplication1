using Microsoft.EntityFrameworkCore;
using System.Reflection;

namespace WebApplication1.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Загружаем все классы из Models.Generated
            var assembly = Assembly.GetExecutingAssembly();
            var generatedEntities = assembly
                .GetTypes()
                .Where(t => t.IsClass &&
                            t.Namespace == "WebApplication1.Models.Generated" &&
                            !t.IsAbstract)
                .ToList();

            // Регистрируем их как Entity-типы
            foreach (var entityType in generatedEntities)
            {
                modelBuilder.Entity(entityType);
            }
        }
    }
}