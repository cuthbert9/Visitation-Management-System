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

if (string.IsNullOrWhiteSpace(connectionString))
{
    throw new InvalidOperationException("Connection string 'DefaultConnection' was not found.");
}

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(connectionString)
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
