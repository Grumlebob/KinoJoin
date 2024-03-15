using Application.Interfaces;
using Infrastructure.Services;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

builder.Services.AddScoped<IKinoDkService, KinoDkService>();

builder.Services.AddHttpClient<IKinoJoinHttpClient, KinoJoinHttpClient>(client =>
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
