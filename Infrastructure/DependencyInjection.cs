using Infrastructure.Persistence;
using Infrastructure.Services;

namespace Infrastructure;

public static class DependencyInjection
{
    public static void AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration
    )
    {
        services.AddScoped<IJoinEventService, JoinEventService>();
        services.AddScoped<IKinoJoinService, KinoJoinService>();
        services.AddScoped<IFetchNewestKinoDkDataService, FetchNewestKinoDkDataService>();
        services.AddDbContextFactory<KinoContext>(options =>
        {
            var secret = configuration["PostgresConnection"];
            options.UseNpgsql(secret);
            options.EnableDetailedErrors();
        });
    }
}
