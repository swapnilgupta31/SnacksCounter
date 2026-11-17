using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using SnackCounter.Infrastructure.Data;

namespace SnackCounter.Tests.TestHelpers;

public static class TestDbFactory
{
    /// <summary>
    /// Creates an isolated SQLite database for testing.
    /// Uses a named shared-cache in-memory connection so multiple DbContext instances
    /// can share the same in-memory database within a test (needed for concurrency tests).
    /// </summary>
    public static SnackCounterDbContext Create(string? dbName = null)
    {
        var name = dbName ?? Guid.NewGuid().ToString("N");
        // Use a persistent connection to keep the in-memory DB alive
        var connection = new SqliteConnection($"Data Source={name};Mode=Memory;Cache=Shared");
        connection.Open();

        var options = new DbContextOptionsBuilder<SnackCounterDbContext>()
            .UseSqlite(connection)
            .Options;

        var db = new SnackCounterDbContext(options);
        db.Database.EnsureCreated();
        return db;
    }
}
