using ABR.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ABR.Infrastructure.Tests;

public static class TestDbContextFactory
{
    public static AbrDbContext Create(string? databaseName = null)
    {
        var options = new DbContextOptionsBuilder<AbrDbContext>()
            .UseInMemoryDatabase(databaseName ?? Guid.NewGuid().ToString())
            .Options;
        return new AbrDbContext(options);
    }
}
