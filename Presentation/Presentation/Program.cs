using Application;
using Carter;
using Infrastructure;
using Presentation.Client;
using Presentation.Components;
using _Imports = Presentation.Client._Imports;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents().AddInteractiveWebAssemblyComponents();
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.AddCors(options =>
{
    options.AddPolicy(
        "AllowAllOrigins",
        corsPolicyBuilder =>
        {
            corsPolicyBuilder.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod();
        }
    );
});

builder.Services.AddCarter();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseWebAssemblyDebugging();
}
else
{
    app.UseExceptionHandler("/Error", true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

if (!GlobalSettings.HostOnNgrokWithNoHttpsAndSetDefaultUser)
{
    app.UseHttpsRedirection();
}

app.UseStaticFiles();
app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveWebAssemblyRenderMode()
    .AddAdditionalAssemblies(typeof(_Imports).Assembly);

app.MapCarter();

app.Run();

//A hacky solution to use Testcontainers with WebApplication.CreateBuilder for integration tests
public partial class Program;
