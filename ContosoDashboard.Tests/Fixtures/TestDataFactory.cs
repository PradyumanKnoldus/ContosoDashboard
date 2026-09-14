using ContosoDashboard.Data;
using Microsoft.EntityFrameworkCore;

namespace ContosoDashboard.Tests.Fixtures;

public static class TestDataFactory
{
    public static ApplicationDbContext CreateContext(string name)
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(name)
            .Options;
        var context = new ApplicationDbContext(options);
        context.Database.EnsureCreated();
        return context;
    }
}