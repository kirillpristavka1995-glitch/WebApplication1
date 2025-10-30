using Microsoft.EntityFrameworkCore;
using WebApplication1;
using WebApplication1.Data;
using WebApplication1.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddScoped<DynamicCompilerService>();

builder.Services.AddSingleton<RoslynGenerator>();

// ✅ Подключаем EF Core
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SchemaFilter<SchemaFilter>();
    options.EnableAnnotations(); // важно, чтобы [SwaggerSchema] работал
});

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors(); 

app.MapControllers();
app.Run();