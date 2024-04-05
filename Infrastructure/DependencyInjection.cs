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
        services.AddScoped<IKinoJoinDbService, KinoJoinDbService>();
        services.AddScoped<IFetchNewestKinoDkDataService, FetchNewestKinoDkDataService>();
        services.AddScoped<IUserInfoService, UserInfoService>();
        services.AddScoped<ICalendarService, CalendarService>();
        services.AddDbContextFactory<KinoContext>(options =>
        {
            var secret = configuration["PostgresConnection"];
            options.UseNpgsql(secret);
            options.EnableDetailedErrors();
        });
    }
}
