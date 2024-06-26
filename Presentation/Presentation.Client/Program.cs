using Application.Interfaces;
using Application.Modules;
using Infrastructure.ExternalServices.KinoAPI;
using Infrastructure.ExternalServices.Users;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Sqids;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

builder.Services.AddScoped<IFilterApiHandler, FilterApiHandler>();
builder.Services.AddScoped<IUserInfoService, UserInfoService>();
builder.Services.AddScoped<ICalendarService, CalendarService>();

builder.Services.AddSingleton(new SqidsEncoder<int>(new SqidsOptions { MinLength = 6 }));

builder.Services.AddHttpClient<KinoJoinHttpClient>(client =>
{
    client.BaseAddress = new Uri(builder.HostEnvironment.BaseAddress);
});

builder.Services.AddOidcAuthentication(options =>
{
    builder.Configuration.Bind("Auth0", options.ProviderOptions);
    options.ProviderOptions.ResponseType = "code";
    options.ProviderOptions.DefaultScopes.Add("email");
});

builder.Services.AddCascadingAuthenticationState();

await builder.Build().RunAsync();
