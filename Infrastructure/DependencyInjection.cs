using Infrastructure.Identity;
using Infrastructure.KinoAPI;

namespace Infrastructure;

public static class DependencyInjection
{
    public static void AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration
    )
    {
        services.AddScoped<IUserInfoService, UserInfoService>();
        services.AddDbContextFactory<KinoContext>(options =>
        {
            var secret = configuration["PostgresConnection"];
            options.UseNpgsql(secret);
            options.EnableDetailedErrors();
        });
        services.AddScoped<IKinoContext>(provider => provider.GetRequiredService<KinoContext>());
        services.AddScoped<IFetchNewestKinoDkDataService, FetchNewestKinoDkDataService>();
    }
}
