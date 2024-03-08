using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Presentation.Client.NamedHttpClients;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

builder.Services.AddHttpClient<IJoinEventHttpClient, JoinEventHttpClient>(client =>
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
