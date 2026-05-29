using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using UserManagement.Core.Interfaces;
using UserManagement.Core.Services;
using UserManagement.Infrastructure;
using UserManagement.Infrastructure.Data;

namespace UserManagement.UnitTests.Infrastructure;

public class DependencyInjectionTests
{
    private static IConfiguration BuildConfig(string provider, string? connectionString = null)
    {
        var data = new Dictionary<string, string?>
        {
            ["Database:Provider"] = provider,
            ["ConnectionStrings:DefaultConnection"] = connectionString
                ?? $"Data Source={Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + ".db")}"
        };
        return new ConfigurationBuilder().AddInMemoryCollection(data).Build();
    }

    [Theory]
    [InlineData("Sqlite")]
    [InlineData("sqlserver")]
    public void AddInfrastructure_RegistersUserServices(string provider)
    {
        var services = new ServiceCollection();
        services.AddInfrastructure(BuildConfig(provider));

        using var sp = services.BuildServiceProvider();
        using var scope = sp.CreateScope();

        Assert.IsAssignableFrom<IUserRepository>(scope.ServiceProvider.GetRequiredService<IUserRepository>());
        Assert.IsAssignableFrom<IUserService>(scope.ServiceProvider.GetRequiredService<IUserService>());
    }

    [Fact]
    public void AddInfrastructure_UsesSqlite_WhenProviderNotSpecified()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = "Data Source=test-default.db"
            })
            .Build();

        var services = new ServiceCollection();
        services.AddInfrastructure(config);

        using var sp = services.BuildServiceProvider();
        var db = sp.GetRequiredService<AppDbContext>();
        Assert.Contains("Sqlite", db.Database.ProviderName, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task MigrateDatabaseAsync_AppliesMigrations()
    {
        var dbPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + ".db");
        var services = new ServiceCollection();
        services.AddInfrastructure(BuildConfig("Sqlite", $"Data Source={dbPath}"));

        var provider = services.BuildServiceProvider();
        await provider.MigrateDatabaseAsync();

        using var scope = provider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        Assert.True(await db.Database.CanConnectAsync());

        try { File.Delete(dbPath); } catch { /* best effort cleanup */ }
    }
}
