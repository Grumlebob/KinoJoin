namespace Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration
    )
    {
        services.AddDbContextFactory<MonkeyContext>(options =>
        {
            var secret = configuration["MonkeyConnection"];
            options.UseNpgsql(secret);
            options.EnableDetailedErrors();
        });
        return services;
    }
}
