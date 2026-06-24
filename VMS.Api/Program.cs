using System.IO;
using Microsoft.EntityFrameworkCore;
using VisitorManagementSystem.Infrastructure.Data;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddCors(options =>
{
    options.AddPolicy("WebClient", policy =>
    {
        policy
            .WithOrigins(
                "http://localhost:5000",
                "http://127.0.0.1:5000",
                "http://localhost:5100",
                "http://127.0.0.1:5100")
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
var builderRoot = builder.Environment.ContentRootPath;

if (string.IsNullOrWhiteSpace(connectionString))
{
    throw new InvalidOperationException("Connection string 'DefaultConnection' was not found.");
}

var dataSourcePrefix = "Data Source=";
if (connectionString.StartsWith(dataSourcePrefix, StringComparison.OrdinalIgnoreCase))
{
    var configuredPath = connectionString[dataSourcePrefix.Length..].Trim();

    if (!Path.IsPathRooted(configuredPath))
    {
        var fullPath = Path.GetFullPath(Path.Combine(builderRoot, configuredPath));
        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
        connectionString = $"{dataSourcePrefix}{fullPath}";
    }
}

Console.WriteLine($"Using SQLite database: {connectionString}");

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(connectionString)
);

var app = builder.Build();

app.UseCors("WebClient");
app.UseAuthorization();
app.MapControllers();

app.Run();
