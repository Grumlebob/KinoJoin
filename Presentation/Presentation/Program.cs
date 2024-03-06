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
builder.Services.AddScoped<IJoinEventService, JoinEventService>();
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
        "https://api.kino.dk/ticketflow/showtimes?sort=most_purchased&cinemas=38?region=content&format=json";

    var client = new HttpClient();
    var json = await client.GetStringAsync(apiString);

    var apiResultObject = JsonConvert.DeserializeObject<Root>(json);

    var facets = apiResultObject!.Content.Content.Facets;

    // Cinemas
    foreach (var cinemaOption in facets.Cinemas.Options)
    {
        var cinema = await context.Cinemas.FindAsync(cinemaOption.Key);
        if (cinema == null)
        {
            context.Cinemas.Add(new Cinema
            {
                Id = cinemaOption.Key,
                Name = cinemaOption.Value
            });
        }
        else
        {
            cinema.Name = cinemaOption.Value;
        }
    }

    //VersionTags
    foreach (var versionOption in facets.Versions.Options)
    {
        var version = await context.Versions.Where(v => v.Type == versionOption.Value).FirstOrDefaultAsync();
        if (version == null)
        {
            await context.Versions.AddAsync(new VersionTag
            {
                Type = versionOption.Value
            });
        }

        await context.SaveChangesAsync();
    }

    //Genres
    foreach (var genreOption in facets.Genres.Options)
    {
        var genre = await context.Genres.FindAsync(genreOption.Key);
        if (genre == null)
        {
            context.Genres.Add(new Genre
            {
                Id = genreOption.Key,
                Name = genreOption.Value
            });
        }
        else
        {
            genre.Name = genreOption.Value;
        }
    }
    
    //MOVIES
    //several cinemas may pose the same movie. No need to create the movie object every time
    Dictionary<int, string> _movieIdsToNames = new();
    var existingMovies = new Dictionary<int, Movie>();
    foreach (var movieOption in apiResultObject.Content.Content.Facets.Movies.Options)
    {
        _movieIdsToNames.Add(movieOption.Key, movieOption.Value);
    }

    foreach (var jsonCinema in apiResultObject.Content.Content.Content.Content)
    {
        foreach (var jsonMovie in
                 jsonCinema.Movies.Where(jsonMovie =>
                     _movieIdsToNames
                         .ContainsKey(jsonMovie
                             .Id))) //if not contains key it is not a movie (there are events with different ids)
        {
            int.TryParse(jsonMovie.Content.FieldPlayingTime, out var duration);
            if (!existingMovies.TryGetValue(jsonMovie.Id,
                    out var movieObject)) //use existing movie object or create new
            {
                movieObject = new Movie
                {
                    Id = jsonMovie.Id,
                    Title = _movieIdsToNames[jsonMovie.Id],
                    PremiereDate = jsonMovie.Content.FieldPremiere,
                    KinoURL = jsonMovie.Content.URL,
                    AgeRating = jsonMovie.Content.FieldCensorshipIcon,
                    ImageUrl = jsonMovie.Content.FieldPoster.FieldMediaImage.Sources[0].Srcset,
                    Duration = duration,
                    Showtimes = new List<Showtime>()
                };
                existingMovies.Add(movieObject.Id, movieObject);
            }
        }
    }

    foreach (var movie in existingMovies.Values)
    {
        var existingMovie = await context.Movies.FindAsync(movie.Id);
        if (existingMovie == null)
        {
            context.Movies.Add(movie);
        }
        else
        {
            existingMovie.Title = movie.Title;
            existingMovie.PremiereDate = movie.PremiereDate;
            existingMovie.KinoURL = movie.KinoURL;
            existingMovie.AgeRating = movie.AgeRating;
            existingMovie.ImageUrl = movie.ImageUrl;
            existingMovie.Duration = movie.Duration;
        }
    }


    await context.SaveChangesAsync();
}

app.Run();

//A hacky solution to use Testcontainers with WebApplication.CreateBuilder for integration tests
public partial class Program
{
}