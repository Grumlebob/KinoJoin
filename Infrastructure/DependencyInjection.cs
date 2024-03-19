using Infrastructure.Persistence;

namespace Infrastructure;

public static class DependencyInjection
{
    public static void AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration
    )
    {
        services.AddDbContextFactory<KinoContext>(options =>
        {
            var secret = configuration["PostgresConnection"];
            options.UseNpgsql(secret);
            options.EnableDetailedErrors();
        });
    }
}
