using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using VisitorManagementSystem.Infrastructure.Data;

namespace VMS.Tests.Common;

/// <summary>
/// Backs an <see cref="AppDbContext"/> with a real SQLite database that lives only in memory,
/// so controller/service tests exercise real query translation, constraints and unique indexes
/// instead of the looser semantics of the EF Core InMemory provider.
/// </summary>
public sealed class SqliteInMemoryDbContextFactory : IDisposable
{
    private readonly SqliteConnection _connection;

    public SqliteInMemoryDbContextFactory()
    {
        _connection = new SqliteConnection("Filename=:memory:");
        _connection.Open();

        using var context = CreateContext();
        context.Database.EnsureCreated();
    }

    public AppDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(_connection)
            .Options;

        return new AppDbContext(options);
    }

    public void Dispose() => _connection.Dispose();
}
