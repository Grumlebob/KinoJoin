namespace Infrastructure.KinoAPI;

public class FetchNewestKinoDkDataService(IKinoContext context) : IFetchNewestKinoDkDataService
{
    private readonly HttpClient _httpClient = new();

    public async Task UpdateBaseDataFromKinoDk(int lowestCinemaId, int highestCinemaId)
    {
        var movieIdsToNames = new Dictionary<int, string>();

        //Several cinemas may have the same movie. No need to create the movie object every time. Instead we save unique movies in a dictionary,
        //and add them to the database after all cinemas have been iterated.
        var movieIdsToObjects = new Dictionary<int, Movie>();

        for (var i = lowestCinemaId; i <= highestCinemaId; i++)
        {
            try
            {
                var apiString =
                    $"https://api.kino.dk/ticketflow/showtimes?sort=most_purchased&cinemas={i}&region=content&format=json";

                var json = await _httpClient.GetStringAsync(apiString);

                var apiResultObject = JsonConvert.DeserializeObject<ShowtimeApiRoot>(json);

                var facets = apiResultObject!.ShowtimeApiContent.ShowtimeApiFacets;

                //facets are unchanged by the query. only save this data once
                if (i == lowestCinemaId)
                {
                    var cinemas = facets.ShowtimeApiCinemas.Options.Select(o => new Cinema
                    {
                        Id = o.Key,
                        Name = o.Value
                    });
                    await context.Cinemas.UpsertRange(cinemas).RunAsync();

                    var versions = facets.Versions.Options.Select(o => new VersionTag
                    {
                        Type = o.Value
                    });
                    await context.Versions.UpsertRange(versions).On(v => v.Type).RunAsync();

                    var genres = facets.ShowtimeApiGenres.Options.Select(o => new Genre
                    {
                        Id = o.Key,
                        Name = o.Value
                    });
                    await context.Genres.UpsertRange(genres).RunAsync();

                    //more data is added throughout the loop iterations
                    movieIdsToNames =
                        apiResultObject.ShowtimeApiContent.ShowtimeApiFacets.Movies.Options.ToDictionary(
                            movieOption => movieOption.Key,
                            movieOption => movieOption.Value
                        );
                }

                //Add movies to movieIdsToObjects
                //if movieIdsToNames does not contain the id it may be a different kind of event like "særvisninger"
                foreach (
                    var jsonMovie in apiResultObject.ShowtimeApiContent.ShowtimeApiContent.Content.SelectMany(
                        jsonCinema =>
                            jsonCinema.Movies.Where(jsonMovie =>
                                movieIdsToNames.ContainsKey(jsonMovie.Id)
                            )
                    )
                )
                {
                    if (movieIdsToObjects.TryGetValue(jsonMovie.Id, out var movieObject))
                        continue; //already added

                    if (!int.TryParse(jsonMovie.Content.FieldPlayingTime, out var duration))
                        duration = 0;
                    string? imageUrl = null;
                    if (
                        jsonMovie.Content.ShowtimeApiFieldPoster.FieldMediaImage?.Sources != null
                        && jsonMovie.Content.ShowtimeApiFieldPoster.FieldMediaImage.Sources.Any()
                    )
                    {
                        imageUrl = jsonMovie
                            .Content
                            .ShowtimeApiFieldPoster
                            .FieldMediaImage
                            .Sources[0]
                            .Srcset;
                    }

                    movieObject = new Movie
                    {
                        Id = jsonMovie.Id,
                        Title = movieIdsToNames[jsonMovie.Id],
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
                        DurationInMinutes = duration,
                    };
                    movieIdsToObjects.Add(movieObject.Id, movieObject);
                }
            }
            catch (Exception)
            {
                throw new Exception(
                    $"Failed to fetch data for cinema {i}. Skipping to next. Usually Kino.dk is down 5% of the day."
                );
            }

            //Save age ratings and movies to database

            List<AgeRating> ageRatings = movieIdsToObjects
                .Values.Where(v => v.AgeRating != null)
                .Select(v => v.AgeRating)
                .DistinctBy(a => a!.Censorship)
                .ToList()!;
            await context.AgeRatings.UpsertRange(ageRatings).On(a => a.Censorship).RunAsync();

            var ageRatingsFromDb = await context.AgeRatings.ToListAsync();

            foreach (var movie in movieIdsToObjects.Values.Where(m => m.AgeRating != null))
            {
                movie.AgeRatingId = ageRatingsFromDb
                    .First(a => a.Censorship == movie.AgeRating!.Censorship)
                    .Id;
            }

            await context.Movies.UpsertRange(movieIdsToObjects.Values).RunAsync();
        }
    }
}
