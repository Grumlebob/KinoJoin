using System.Data.Common;
using System.Security.Cryptography;
using Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Respawn;
using Testcontainers.PostgreSql;

namespace Test.KinoJoin;

//WebApplicationFactory is a class that allows us to create a test server for our application in memory, but setup with real dependencies.
public class KinoJoinApiWebAppFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    /// The entire test environment uses a single connection to the database. This means that all the tests have to be done by the time
    /// the connection is closed. <seealso cref="UpdateAllBaseDataFromKinoDk_ShouldReturnOk_IfUpdateSucceeds">hejsa</seealso>
    /// The reason why this is necessary is because preseeding of the database is slow and will end up being cancelled
    /// if the connection is closed before terminating
    private const int MaxWaittimeMinutes = 5;

    private readonly PostgreSqlContainer _dbContainer = new PostgreSqlBuilder()
        .WithImage("postgres:latest")
        .Build();

    //Default! cause we are not initializing it here, but in the InitializeAsync method
    private DbConnection _dbConnection = default!;
    private Respawner _respawner = default!;
    public HttpClient HttpClient { get; private set; } = default!;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        //Setup dependency injection for this test application
        builder.ConfigureTestServices(services =>
        {
            //Remove the existing KinoContext from the services
            var descriptor = services.SingleOrDefault(d =>
                d.ServiceType == typeof(DbContextOptions<KinoContext>)
            );

            if (descriptor != null)
            {
                services.Remove(descriptor);
            }

            //Setup our new KinoContext connection to our docker postgres container
            services.AddDbContext<KinoContext>(
                options =>
                {
                    options.UseNpgsql(_dbContainer.GetConnectionString());
                },
                ServiceLifetime.Singleton
            ); // Lifetime must be Singleton to work with TestContainers
        });
    }

    public async Task ResetDatabaseAsync()
    {
        await _respawner.ResetAsync(_dbConnection);
    }

    public async Task InitializeAsync()
    {
        await _dbContainer.StartAsync();

        _dbConnection = new NpgsqlConnection(_dbContainer.GetConnectionString());

        HttpClient = CreateClient();
        //Seeding data can take a long time, so we set a longer timeout
        HttpClient.Timeout = TimeSpan.FromMinutes(MaxWaittimeMinutes);

        //THIS IS WHERE YOU CAN ADD SEED DATA
        using var scope = Services.CreateScope();
        var services = scope.ServiceProvider;
        var context = services.GetRequiredService<KinoContext>();
        await context.Database.EnsureCreatedAsync();
        await InitializeRespawner();
    }

    private async Task InitializeRespawner()
    {
        await _dbConnection.OpenAsync();
        _respawner = await Respawner.CreateAsync(
            _dbConnection,
            new RespawnerOptions()
            {
                DbAdapter = DbAdapter.Postgres,
                SchemasToInclude = new[] { "public" }
            }
        );
    }

    //"New": to tell compiler that this is a new DisposeAsync method
    public new Task DisposeAsync()
    {
        return _dbContainer.StopAsync();
    }
}
