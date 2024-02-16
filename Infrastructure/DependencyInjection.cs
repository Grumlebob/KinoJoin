using Microsoft.Extensions.DependencyInjection;
using Infrastructure.Database;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services)
    {
        services.AddDbContextFactory<DataContext>(options =>
        {
            var secret = builder.Configuration["PostgresConnection"];
            options.UseNpgsql(secret);
            options.EnableDetailedErrors();
        });
        return services;
    }
}
