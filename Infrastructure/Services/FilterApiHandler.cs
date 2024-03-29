﻿using System.Globalization;
using System.Text;
using Domain.ExternalApiModels;
using Newtonsoft.Json;

namespace Infrastructure.Services;

public class FilterApiHandler : IFilterApiHandler
{
    private readonly HttpClient _httpClient = new();

    private const string BaseUrl =
        "https://api.kino.dk/ticketflow/showtimes?region=content&format=json";

    public async Task<(
        List<Showtime> showtimes,
        List<Movie> moviesWithoutShowtimes
    )> GetShowtimesFromFilters(
        ICollection<int>? cinemaIds = null,
        ICollection<int>? movieIds = null,
        ICollection<int>? genreIds = null,
        DateTime? fromDate = null,
        DateTime? toDate = null
    )
    {
        cinemaIds ??= [];
        movieIds ??= [];
        genreIds ??= [];
        fromDate ??= DateTime.Now;
        toDate ??= DateTime.Now.AddMinutes(1);

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

        filterStringBuilder.Append($"&date={fromDate.Value.ToString("O")}");
        filterStringBuilder.Append($"&date={toDate.Value.ToString("O")}");

        var apiString = BaseUrl + filterStringBuilder;

        var json = await _httpClient.GetStringAsync(apiString);
        var apiResultObject = JsonConvert.DeserializeObject<ShowtimeApiRoot>(json);
        if (apiResultObject == null)
            return ([], []);

        //names are stored in facets
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
            var cinemaObject = new Cinema
            {
                Id = jsonCinema.Id,
                Name = cinemaIdsToNames[jsonCinema.Id]
            };

            foreach (
                var jsonMovie in jsonCinema.Movies.Where(jsonMovie =>
                    movieIdsToNames.ContainsKey(jsonMovie.Id)
                )
            ) //if not contains key it is not a movie (there are events with different ids)
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
                        ImageUrl =
                            jsonMovie.Content.ShowtimeApiFieldPoster.FieldMediaImage?.Sources?[
                                0
                            ].Srcset.Replace(
                                "https://api.kino.dk/sites/kino.dk/files/styles/isg_focal_point_356_534/public/",
                                ""
                            ),
                        Duration = duration,
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

                            if (
                                DateTime.TryParseExact(
                                    dateString,
                                    dateTimeFormat,
                                    CultureInfo.InvariantCulture,
                                    DateTimeStyles.None,
                                    out var parsedDate
                                )
                            )
                            {
                                //Couldn't get the api to filter on date. Do it manually
                                if (fromDate != DateTime.MinValue && parsedDate < fromDate)
                                    continue;

                                if (toDate != DateTime.MinValue && parsedDate > toDate)
                                    continue;
                            }

                            var playtimeObject = new Playtime { StartTime = parsedDate };

                            var showtimeObject = new Showtime
                            {
                                Id = jsonShowtime.Id,
                                Movie = movieObject,
                                Cinema = cinemaObject,
                                VersionTag = versionObject,
                                Room = roomObject,
                                Playtime = playtimeObject
                            };
                            showtimes.Add(showtimeObject);
                        }
                    }
                }
            }
        }

        //include movies that had no show times
        var filterString = new StringBuilder("&sort=most_purchased");
        var notIncludedMovieIds = movieIds
            .Where(movieId => showtimes.All(f => f.Movie.Id != movieId))
            .ToList();
        var index = 0;
        foreach (var movieId in notIncludedMovieIds)
        {
            filterString.Append($"&movies[{index++}]={movieId}");
        }

        if (notIncludedMovieIds.Count == 0)
            return (showtimes, []); //all movies had showtimes

        var movieApiString = BaseUrl + filterString;
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
                ImageUrl = movie.Content.ShowtimeApiFieldPoster.FieldMediaImage?.Sources?[
                    0
                ].Srcset.Replace(
                    "https://api.kino.dk/sites/kino.dk/files/styles/isg_focal_point_356_534/public/",
                    ""
                ),
                Duration = duration
            };
            missingMovies.Add(movieObject);
        }

        return (showtimes, missingMovies);
    }
}
