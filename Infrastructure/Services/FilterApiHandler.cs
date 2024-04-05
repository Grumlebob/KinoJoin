using System.Globalization;
using System.Text;
using System.Web;
using Domain.ExternalApiModels;
using Newtonsoft.Json;

namespace Infrastructure.Services;

public class FilterApiHandler : IFilterApiHandler
{
    private readonly HttpClient _httpClient = new();

    private const string ApiBaseUrl =
        "https://api.kino.dk/ticketflow/showtimes?region=content&format=json";

    public async Task<(
        List<Showtime> showtimes,
        List<Movie> moviesWithoutShowtimes
    )> GetShowtimesFromFilters(
        ICollection<int> cinemaIds = null!,
        ICollection<int> movieIds = null!,
        ICollection<int> genreIds = null!,
        DateTime fromDate = default,
        DateTime toDate = default
    )
    {
        cinemaIds ??= [];
        movieIds ??= [];
        genreIds ??= [];
        if (fromDate == default)
            fromDate = DateTime.Today;
        if (toDate == default)
            toDate = DateTime.Today.AddYears(1);

        fromDate = fromDate.Date;
        toDate = toDate.Date;

        var filterStringBuilder = new StringBuilder("&sort=most_purchased");
        var cinemaList = cinemaIds.ToList();
        for (var i = 0; i < cinemaIds.Count; i++)
        {
            filterStringBuilder.Append($"&cinemas[{i}]={cinemaList[i]}");
        }

        var movieList = movieIds.ToList();
        for (var i = 0; i < movieIds.Count; i++)
        {
            filterStringBuilder.Append($"&movies[{i}]={movieList[i]}");
        }

        var genreList = genreIds.ToList();
        for (var i = 0; i < genreIds.Count; i++)
        {
            filterStringBuilder.Append($"&genres[{i}]={genreList[i]}");
        }

        filterStringBuilder.Append($"&date[start]={fromDate.ToString("s")}"); //format: 2008-04-18T06:30:00, this is the format the kino API expects
        filterStringBuilder.Append($"&date[end]={toDate.ToString("s")}");

        var apiString = ApiBaseUrl + filterStringBuilder;

        var json = await _httpClient.GetStringAsync(apiString);
        var apiResultObject = JsonConvert.DeserializeObject<ShowtimeApiRoot>(json);
        if (apiResultObject == null)
            return ([], []);

        //names are stored in facets object in kino.dk api data
        var cinemaIdsToNames =
            apiResultObject.ShowtimeApiContent.ShowtimeApiFacets.ShowtimeApiCinemas.Options.ToDictionary(
                cinemaOption => cinemaOption.Key,
                cinemaOption => cinemaOption.Value
            );
        var movieIdsToNames =
            apiResultObject.ShowtimeApiContent.ShowtimeApiFacets.Movies.Options.ToDictionary(
                movieOption => movieOption.Key,
                movieOption => movieOption.Value
            );

        var showtimes = new List<Showtime>();
        var existingMovies = new Dictionary<int, Movie>(); //several cinemas may show the same movie. No need to create the movie object every time

        foreach (var jsonCinema in apiResultObject.ShowtimeApiContent.ShowtimeApiContent.Content)
        {
            //if cinemaIdsToNames does not contain the id it may be a different unhandled entity, such as "særvisninger"
            if (!cinemaIdsToNames.ContainsKey(jsonCinema.Id))
                continue;

            var cinemaObject = new Cinema
            {
                Id = jsonCinema.Id,
                Name = cinemaIdsToNames[jsonCinema.Id]
            };

            foreach (
                var jsonMovie in jsonCinema.Movies.Where(jsonMovie =>
                    movieIdsToNames.ContainsKey(jsonMovie.Id)
                )
            ) //if not contains key it is not a movie (there are events with different ids, example "særvisninger" are excluded)
            {
                if (!int.TryParse(jsonMovie.Content.FieldPlayingTime, out var duration))
                    duration = 0;
                if (!existingMovies.TryGetValue(jsonMovie.Id, out var movieObject)) //use existing movie object or create new
                {
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
                        ImageUrl = jsonMovie
                            .Content
                            .ShowtimeApiFieldPoster
                            .FieldMediaImage
                            ?.Sources
                            ?[0]
                            .Srcset,
                        DurationInMinutes = duration,
                    };
                    existingMovies.Add(movieObject.Id, movieObject);
                }

                foreach (var jsonVersion in jsonMovie.Versions)
                {
                    if (
                        jsonVersion.Label.Contains(
                            "lukket forestilling",
                            StringComparison.CurrentCultureIgnoreCase
                        )
                    )
                        continue;

                    var versionObject = new VersionTag { Type = jsonVersion.Label };

                    foreach (var jsonDate in jsonVersion.Dates)
                    {
                        foreach (
                            var jsonShowtime in jsonDate.Showtimes.Where(s => s.AvailableSeats > 0)
                        )
                        {
                            var roomObject = new Room
                            {
                                Id = jsonShowtime.ShowtimeApiRoomContent.Id,
                                Name = jsonShowtime.ShowtimeApiRoomContent.Label
                            };

                            var dateString = jsonDate.Date + " " + jsonShowtime.Time;
                            var commaIndex = dateString.IndexOf(',');
                            if (commaIndex > 0)
                            {
                                dateString = dateString[(commaIndex + 2)..]; //"fre, 08/03" -> "08/03"
                            }

                            const string dateTimeFormat = "dd/MM HH:mm";

                            DateTime.TryParseExact(
                                dateString,
                                dateTimeFormat,
                                CultureInfo.InvariantCulture,
                                DateTimeStyles.None,
                                out var parsedDate
                            );

                            var playtimeObject = new Playtime { StartTime = parsedDate };

                            var showtimeObject = new Showtime
                            {
                                Id = jsonShowtime.Id,
                                Movie = movieObject,
                                MovieId = movieObject.Id,
                                Cinema = cinemaObject,
                                CinemaId = cinemaObject.Id,
                                VersionTag = versionObject,
                                VersionTagId = versionObject.Id,
                                Room = roomObject,
                                RoomId = roomObject.Id,
                                Playtime = playtimeObject,
                                PlaytimeId = playtimeObject.Id,
                            };
                            showtimes.Add(showtimeObject);
                        }
                    }
                }
            }
        }

        //include movies that had no show times
        var notIncludedMovieIds = movieIds
            .Where(movieId => showtimes.All(f => f.Movie.Id != movieId))
            .ToList();
        var index = 0;

        if (notIncludedMovieIds.Count == 0)
            return (showtimes, []); //all movies had showtimes

        //we call the API again to get information on the missing movies, now without the rest of the filters, to make sure we get all the movies
        var filterString = new StringBuilder("&sort=most_purchased");
        foreach (var movieId in notIncludedMovieIds)
        {
            filterString.Append($"&movies[{index++}]={movieId}");
        }

        var movieApiString = ApiBaseUrl + filterString;
        var movieJson = await _httpClient.GetStringAsync(movieApiString);
        var root = JsonConvert.DeserializeObject<ShowtimeApiRoot>(movieJson);
        if (root == null)
            return (showtimes, []);

        List<Movie> missingMovies = [];
        foreach (
            var movie in root.ShowtimeApiContent.ShowtimeApiContent.Content.SelectMany(cinema =>
                cinema.Movies.Where(movie => missingMovies.All(m => m.Id != movie.Id))
            )
        )
        {
            if (!int.TryParse(movie.Content.FieldPlayingTime, out var duration))
                duration = 0;

            var movieObject = new Movie
            {
                Id = movie.Id,
                Title = movieIdsToNames[movie.Id],
                PremiereDate = movie.Content.FieldPremiere,
                KinoUrl = movie.Content.Url,
                AgeRating =
                    movie.Content.FieldCensorshipIcon == null
                        ? null
                        : new AgeRating { Censorship = movie.Content.FieldCensorshipIcon },
                ImageUrl = movie.Content.ShowtimeApiFieldPoster.FieldMediaImage?.Sources?[0].Srcset,
                DurationInMinutes = duration
            };
            missingMovies.Add(movieObject);
        }

        return (showtimes, missingMovies);
    }

    public string ConvertFiltersToUrlString(
        ICollection<int> cinemaIds = null!,
        ICollection<int> movieIds = null!,
        ICollection<int> genreIds = null!,
        DateTime fromDate = default,
        DateTime toDate = default
    )
    {
        cinemaIds ??= [];
        movieIds ??= [];
        genreIds ??= [];
        if (fromDate == default)
            fromDate = DateTime.Today;
        if (toDate == default)
            toDate = DateTime.Today.AddYears(1);

        fromDate = fromDate.Date;
        toDate = toDate.Date;

        var fromDateString = fromDate.ToString("s"); //format: 2008-04-18T06:30:00, this format is needed so there is no slashes in the string, which would be interpreted as paths in the url
        var toDateString = toDate.ToString("s");

        var filterStringBuilder = new StringBuilder("sort=most_purchased");

        foreach (var id in movieIds)
        {
            filterStringBuilder.Append($"&movies={id}");
        }

        foreach (var id in cinemaIds)
        {
            filterStringBuilder.Append($"&cinemas={id}");
        }

        foreach (var id in genreIds)
        {
            filterStringBuilder.Append($"&genres={id}");
        }

        filterStringBuilder.Append($"&date={fromDateString}");
        filterStringBuilder.Append($"&date={toDateString}");

        return filterStringBuilder.ToString();
    }

    public (
        ISet<int> selectedCinemas,
        ISet<int> selectedMovies,
        ISet<int> selectedGenres,
        DateTime startDate,
        DateTime endDate
    ) GetFiltersFromUrlFilterString(string filterString)
    {
        var selectedCinemas = new HashSet<int>();
        var selectedMovies = new HashSet<int>();
        var selectedGenres = new HashSet<int>();
        DateTime startDate = default;
        DateTime endDate = default;

        var query = "?" + filterString;
        var queryParams = HttpUtility.ParseQueryString(query);

        var firstDate = true;
        foreach (var key in queryParams.AllKeys)
        {
            if (key == null)
                continue;
            foreach (var value in queryParams.GetValues(key)!)
            {
                switch (key)
                {
                    case "cinemas" when int.TryParse(value, out var cinemaId):
                        selectedCinemas.Add(cinemaId);
                        break;
                    case "movies" when int.TryParse(value, out var movieId):
                        selectedMovies.Add(movieId);
                        break;
                    case "genres" when int.TryParse(value, out var genreId):
                        selectedGenres.Add(genreId);
                        break;
                    case "date" when DateTime.TryParse(value, out var parsedDate):
                        if (firstDate)
                        {
                            startDate = parsedDate.Date;
                            firstDate = false;
                        }
                        else
                        {
                            endDate = parsedDate.Date;
                        }

                        break;
                }
            }
        }

        return (selectedCinemas, selectedMovies, selectedGenres, startDate, endDate);
    }
}
