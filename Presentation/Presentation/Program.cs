using Application;
using Application.Interfaces;
using Carter;
using Domain.Entities;
using Domain.ExternalApi;
using Infrastructure;
using Infrastructure.Database;
using Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using Presentation;
using Presentation.Components;
using _Imports = Presentation.Client._Imports;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents().AddInteractiveWebAssemblyComponents();
builder.Services.AddScoped<IJoinEventService, JoinEventService>();
builder.Services.AddScoped<IKinoJoinService, KinoJoinService>();
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

app.UseHttpsRedirection();

app.UseStaticFiles();
app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveWebAssemblyRenderMode()
    .AddAdditionalAssemblies(typeof(_Imports).Assembly);

app.MapCarter();

if (GlobalSettings.ShouldPreSeedDatabase)
{
    using var scope = app.Services.CreateScope();
    var context = scope.ServiceProvider.GetRequiredService<KinoContext>();

    var lowestCinemaId = 1; //Hardcoded from kino.dk
    var highestCinemaId = 71; //71; //Hardcoded from kino.dk

    for (int i = lowestCinemaId; i <= highestCinemaId; i++)
    {
        var apiString =
            $"https://api.kino.dk/ticketflow/showtimes?sort=most_purchased&cinemas={i}&region=content&format=json";

        var client = new HttpClient();
        var json = await client.GetStringAsync(apiString);

        try
        {
            var apiResultObject = JsonConvert.DeserializeObject<Root>(json);

            var facets = apiResultObject!.Content.Facets;

            // Cinemas
            foreach (var cinemaOption in facets.Cinemas.Options)
            {
                var cinema = await context.Cinemas.FindAsync(cinemaOption.Key);
                if (cinema == null)
                {
                    context.Cinemas.Add(
                        new Cinema { Id = cinemaOption.Key, Name = cinemaOption.Value }
                    );
                }
                else
                {
                    cinema.Name = cinemaOption.Value;
                }
            }

            //VersionTags
            foreach (var versionOption in facets.Versions.Options)
            {
                var version = await context
                    .Versions.Where(v => v.Type == versionOption.Value)
                    .FirstOrDefaultAsync();
                if (version == null)
                {
                    await context.Versions.AddAsync(new VersionTag { Type = versionOption.Value });
                }

                await context.SaveChangesAsync();
            }

            //Genres
            foreach (var genreOption in facets.Genres.Options)
            {
                var genre = await context.Genres.FindAsync(genreOption.Key);
                if (genre == null)
                {
                    context.Genres.Add(
                        new Genre { Id = genreOption.Key, Name = genreOption.Value }
                    );
                }
                else
                {
                    genre.Name = genreOption.Value;
                }
            }

            //MOVIES
            //several cinemas may pose the same movie. No need to create the movie object every time
            Dictionary<int, string> _movieIdsToNames = new();
            var MoviesOnKinoDk = new Dictionary<int, Movie>();
            foreach (var movieOption in apiResultObject.Content.Facets.Movies.Options)
            {
                _movieIdsToNames.Add(movieOption.Key, movieOption.Value);
            }

            foreach (var jsonCinema in apiResultObject.Content.Content.Content)
            {
                foreach (
                    var jsonMovie in jsonCinema.Movies.Where(jsonMovie =>
                        _movieIdsToNames.ContainsKey(jsonMovie.Id)
                    )
                ) //if not contains key it is not a movie (there are events with different ids)
                {
                    int.TryParse(jsonMovie.Content.FieldPlayingTime, out var duration);
                    if (!MoviesOnKinoDk.TryGetValue(jsonMovie.Id, out var movieObject))
                    {
                        string imageUrl = null;
                        if (
                            jsonMovie?.Content?.FieldPoster?.FieldMediaImage?.Sources != null
                            && jsonMovie.Content.FieldPoster.FieldMediaImage.Sources.Any()
                        )
                        {
                            imageUrl = jsonMovie
                                .Content
                                .FieldPoster
                                .FieldMediaImage
                                .Sources[0]
                                ?.Srcset;
                        }

                        movieObject = new Movie
                        {
                            Id = jsonMovie.Id,
                            Title = _movieIdsToNames[jsonMovie.Id],
                            PremiereDate = jsonMovie.Content.FieldPremiere,
                            KinoURL = jsonMovie.Content.URL,
                            AgeRating =
                                jsonMovie.Content.FieldCensorshipIcon == null
                                    ? null
                                    : new AgeRating
                                    {
                                        Censorship = jsonMovie.Content.FieldCensorshipIcon
                                    },
                            ImageUrl = imageUrl,
                            Duration = duration,
                        };
                        MoviesOnKinoDk.Add(movieObject.Id, movieObject);
                    }
                }
            }

            foreach (var movie in MoviesOnKinoDk.Values)
            {
                if (movie.AgeRating == null)
                {
                    Console.WriteLine("AgeRating is null for movie: " + movie.Title);
                }
                else
                {
                    Console.WriteLine("AgeRating is not null for movie: " + movie.Title);
                }

                var existingMovie = await context.Movies.FindAsync(movie.Id);
                var existingAgeRating = await context.AgeRatings.FirstOrDefaultAsync(m =>
                    movie.AgeRating != null && m.Censorship == movie.AgeRating.Censorship
                );

                if (existingMovie == null)
                {
                    movie.AgeRating = existingAgeRating ?? movie.AgeRating ?? null;
                    context.Movies.Add(movie);
                }
                else
                {
                    existingMovie.Title = movie.Title;
                    existingMovie.PremiereDate = movie.PremiereDate;
                    existingMovie.KinoURL = movie.KinoURL;
                    existingMovie.AgeRating = existingAgeRating ?? movie.AgeRating ?? null;
                    existingMovie.ImageUrl = movie.ImageUrl;
                    existingMovie.Duration = movie.Duration;
                }

                await context.SaveChangesAsync();
            }
        }
        catch (Exception e)
        {
            Console.WriteLine("Preseed error on cinema id: " + i);
            Console.WriteLine(e);
            Thread.Sleep(50000);
        }
    }
}

app.Run();

//A hacky solution to use Testcontainers with WebApplication.CreateBuilder for integration tests
public partial class Program { }
