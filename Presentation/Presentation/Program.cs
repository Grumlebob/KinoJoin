using Application;
using Application.Interfaces;
using Domain.Entities;
using Domain.ExternalApi;
using Infrastructure;
using Infrastructure.Database;
using Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using Presentation.Components;
using Presentation.Endpoints;
using _Imports = Presentation.Client._Imports;

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
    app.UseExceptionHandler("/Error", true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseStaticFiles();
app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveWebAssemblyRenderMode()
    .AddAdditionalAssemblies(typeof(_Imports).Assembly);

app.MapMonkeyEndpoints();
app.MapKinoJoinEndpoints();

using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<KinoContext>();

    var apiString =
        "https://api.kino.dk/ticketflow/showtimes?sort=most_purchased&cinemas=53?region=content&format=json";

    var client = new HttpClient();
    var json = await client.GetStringAsync(apiString);

    var apiResultObject = JsonConvert.DeserializeObject<Root>(json);

    var facets = apiResultObject!.Content.Content.Facets;

    var cinemas = facets.Cinemas.Options.Select(cinemaOption => new Cinema
    {
        Id = cinemaOption.Key,
        Name = cinemaOption.Value
    });

    context.AddRange(cinemas);

    var movies = facets.Movies.Options.Select(movieOption => new Movie
    {
        Id = movieOption.Key,
        Title = movieOption.Value
    });

    context.AddRange(movies);
    var versions = facets.Versions.Options.Select(versionOptions => new VersionTag
    {
        Type = versionOptions.Value
    });

    context.AddRange(versions);

    var genres = facets
        .Genres.Options.Select(genrerOptions => new Genre
        {
            Id = genrerOptions.Key,
            Name = genrerOptions.Value
        })
        .ToList();

    context.AddRange(genres);

    await context.SaveChangesAsync();
}

app.Run();

//A hacky solution to use Testcontainers with WebApplication.CreateBuilder for integration tests
public partial class Program { }
