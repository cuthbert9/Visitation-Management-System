// See https://aka.ms/new-console-template for more information
// Console.WriteLine(" Visitor Management System");


using System.IO;
using Microsoft.EntityFrameworkCore;
using VisitorManagementSystem.Infrastructure.Data;

var builder = WebApplication.CreateBuilder(args);

// Add controllers
builder.Services.AddControllers();

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

// SQLite DB connection
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(connectionString)
);

var app = builder.Build();

app.UseAuthorization();
app.MapControllers();

app.Run();

