using Application.DTO;
using Application.Interfaces;
using Infrastructure.Database;
using Infrastructure.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.PostgreSql;

namespace Test;

public class MonkeyServiceWebAppFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    
    private readonly PostgreSqlContainer _dbContainer = new PostgreSqlBuilder()
        .WithImage("postgres:latest")
        .Build();
    
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        //Setup dependency injection for this test application
        builder.ConfigureTestServices(services =>
        {
            //Remove the existing MonkeyContext from the services
            var descriptor = services.SingleOrDefault(
                d => d.ServiceType ==
                     typeof(DbContextOptions<MonkeyContext>));
            
            if (descriptor != null)
            {
                services.Remove(descriptor);
            }
            
            var configuration = services.BuildServiceProvider().GetService<IConfiguration>();
            
            //Setup our new MonkeyContext connection to our docker postgres container
            services.AddDbContext<MonkeyContext>(options =>
            {
                options.UseNpgsql(configuration!["MonkeyConnection"]);
            }, ServiceLifetime.Singleton); // Lifetime must be Singleton to work with TestContainers
        });

    }
    
    public async Task InitializeAsync()
    {
        await _dbContainer.StartAsync();
        
        //USED FOR SETUP TESTING, THIS IS WHERE YOU CAN ADD SEED DATA
        using var scope = Services.CreateScope();
        var services = scope.ServiceProvider;
        var context = services.GetRequiredService<MonkeyContext>();
        //Ensure the database is created
        await context.Database.MigrateAsync();
        await context.Database.EnsureCreatedAsync();
    }
    
    //New: to tell compiler that this is a new DisposeAsync method
    public new Task DisposeAsync()
    {
        return _dbContainer.StopAsync();
    }
}