
namespace Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        var assembly = typeof(DependencyInjection).Assembly;
        services.AddValidatorsFromAssembly(assembly);
        services.AddScoped<IKinoJoinDbService, KinoJoinDbService>();
        // services.AddScoped<IFetchNewestKinoDkDataService, FetchNewestKinoDkDataService>();
        services.AddScoped<ICalendarService, CalendarService>();
        return services;
    }
}
