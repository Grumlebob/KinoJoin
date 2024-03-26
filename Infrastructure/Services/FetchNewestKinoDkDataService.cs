using Domain.ExternalApiModels;
using Infrastructure.Persistence;
using Newtonsoft.Json;

namespace Infrastructure.Services;

public class FetchNewestKinoDkDataService(KinoContext context) : IFetchNewestKinoDkDataService
{
    private readonly HttpClient _httpClient = new();

    public async Task UpdateBaseDataFromKinoDk(int lowestCinemaId, int highestCinemaId)
    {
        for (int i = lowestCinemaId; i <= highestCinemaId; i++)
        {
            var apiString =
                $"https://api.kino.dk/ticketflow/showtimes?sort=most_purchased&cinemas={i}&region=content&format=json";

            var json = await _httpClient.GetStringAsync(apiString);

            try
            {
                var apiResultObject = JsonConvert.DeserializeObject<ShowtimeApiRoot>(json);

                var facets = apiResultObject!.ShowtimeApiContent.ShowtimeApiFacets;

                // Cinemas
                foreach (var cinemaOption in facets.ShowtimeApiCinemas.Options)
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
                        await context.Versions.AddAsync(
                            new VersionTag { Type = versionOption.Value }
                        );
                    }

                    await context.SaveChangesAsync();
                }

                //Genres
                foreach (var genreOption in facets.ShowtimeApiGenres.Options)
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
                foreach (
                    var movieOption in apiResultObject
                        .ShowtimeApiContent
                        .ShowtimeApiFacets
                        .Movies
                        .Options
                )
                {
                    _movieIdsToNames.Add(movieOption.Key, movieOption.Value);
                }

                foreach (
                    var jsonCinema in apiResultObject.ShowtimeApiContent.ShowtimeApiContent.Content
                )
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
                                jsonMovie?.Content?.ShowtimeApiFieldPoster?.FieldMediaImage?.Sources
                                    != null
                                && jsonMovie.Content.ShowtimeApiFieldPoster.FieldMediaImage.Sources.Any()
                            )
                            {
                                imageUrl = jsonMovie
                                    .Content.ShowtimeApiFieldPoster.FieldMediaImage.Sources[0]
                                    ?.Srcset.Replace(
                                        "https://api.kino.dk/sites/kino.dk/files/styles/isg_focal_point_356_534/public/",
                                        ""
                                    );
                            }

                            movieObject = new Movie
                            {
                                Id = jsonMovie.Id,
                                Title = _movieIdsToNames[jsonMovie.Id],
                                PremiereDate = jsonMovie.Content.FieldPremiere,
                                KinoUrl = jsonMovie.Content.Url,
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
                        existingMovie.KinoUrl = movie.KinoUrl;
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
            }
        }
    }
}
