using System.IO;
using Microsoft.EntityFrameworkCore;
using VisitorManagementSystem.Api.Configuration;
using VisitorManagementSystem.Api.Services;
using VisitorManagementSystem.Infrastructure.Data;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.Configure<SmtpSettings>(builder.Configuration.GetSection("SmtpSettings"));
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<IAuditLogService, AuditLogService>();
builder.Services.AddCors(options =>
{
    options.AddPolicy("WebClient", policy =>
    {
        policy
            .AllowAnyOrigin()
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



// using (var scope = app.Services.CreateScope())
// {
//     var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
//     await AdminSeedRunner.SeedDemoUsersAsync(context);
//     Console.WriteLine("Demo user seed complete.");
// }

app.UseCors("WebClient");
app.UseAuthorization();
app.MapControllers();

app.Run();
