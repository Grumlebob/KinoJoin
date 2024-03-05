using Application;
using Application.Interfaces;
using Infrastructure;
using Infrastructure.Database;
using Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Presentation.Components;
using Presentation.Endpoints;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents().AddInteractiveWebAssemblyComponents();

builder.Services.AddScoped<IMonkeyService, MonkeyService>();
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

// Add CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy(
        "AllowAllOrigins",
        builder =>
        {
            builder
                .AllowAnyOrigin() // This allows requests from any origin
                .AllowAnyHeader()
                .AllowAnyMethod();
            // Do not call AllowCredentials() when using AllowAnyOrigin()
        }
    );
});

builder.Services.AddDbContextFactory<KinoContext>(options =>
{
    var secret = builder.Configuration["PostgresConnection"];
    options.UseNpgsql(secret);
    options.EnableDetailedErrors();
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseWebAssemblyDebugging();
}
else
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseStaticFiles();
app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveWebAssemblyRenderMode()
    .AddAdditionalAssemblies(typeof(Presentation.Client._Imports).Assembly);

app.MapMonkeyEndpoints();
app.MapKinoJoinEndpoints();

app.Run();

//A hacky solution to use Testcontainers with WebApplication.CreateBuilder for integration tests
public partial class Program { }
