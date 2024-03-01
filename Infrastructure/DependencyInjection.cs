﻿namespace Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration
    )
    {
        services.AddDbContextFactory<DataContext>(options =>
        {
            var secret = configuration["PostgresConnection"];
            options.UseNpgsql(secret);
            options.EnableDetailedErrors();
        });
        return services;
    }
}
