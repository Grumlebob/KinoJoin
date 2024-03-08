﻿using System.Data.Common;
using Infrastructure.Database;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Respawn;
using Testcontainers.PostgreSql;

namespace Test.KinoJoin;

//WebApplicationFactory is a class that allows us to create a test server for our application in memory, but setup with real dependencies.
public class KinoJoinApiWebAppFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _dbContainer = new PostgreSqlBuilder()
        .WithImage("postgres:latest")
        .Build();

    //Default! cause we are not initializing it here, but in the InitializeAsync method
    private DbConnection _dbConnection = default!;
    private Respawner _respawner = default!;
    public HttpClient HttpClient { get; private set; } = default!;

    //Config used for secrets
    private IConfiguration Configuration => Services.GetRequiredService<IConfiguration>();

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
                    string? connectionString = Environment.GetEnvironmentVariable("TESTDATABASECONNECTION");
                    if (string.IsNullOrEmpty(connectionString))
                    {
                        connectionString = Configuration["TestDatabaseConnection"]!;
                    }
                    options.UseNpgsql(connectionString);
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

        var mappedPort = _dbContainer.GetMappedPublicPort(5432);
        var containerName = _dbContainer.Name;

        try
        {
            string? connectionString = Environment.GetEnvironmentVariable("TESTDATABASECONNECTION");
            Console.WriteLine($"Connection string after getting from environment: {connectionString[0..40]}");
            Console.WriteLine($"Container name: {containerName}, Mapped port: {mappedPort}");
            
            if (string.IsNullOrEmpty(connectionString))
            {
                connectionString = Configuration["TestDatabaseConnection"];
            }

            _dbConnection = new NpgsqlConnection(connectionString);
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
            throw;
        }
        

        HttpClient = CreateClient();

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
                SchemasToInclude = new[] { "public"}
            }
        );
    }

    //"New": to tell compiler that this is a new DisposeAsync method
    public new Task DisposeAsync()
    {
        return _dbContainer.StopAsync();
    }
}
