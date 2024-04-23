namespace Application.Interfaces;

/// <summary>
/// The main idea of this interface is to handle the filters namely, cinemas, movies, genres and dates.
/// </summary>
public interface IFilterApiHandler
{
    /// <summary>
    /// Use the given filters to call kino.dk's api and fetch their data.
    /// </summary>
    ///<returns>
    /// A tuple with a list of Showtimes matching the filters and a list with all the selected movies which had no matching showtimes
    /// </returns>
    Task<(List<Showtime> showtimes, List<Movie> moviesWithoutShowtimes)> GetShowtimesFromFilters(
        ICollection<int> cinemaIds = null!,
        ICollection<int> movieIds = null!,
        ICollection<int> genreIds = null!,
        DateTime fromDate = default,
        DateTime toDate = default,
        SortOption sortOption = SortOption.Most_viewed
    );

    /// <summary>
    /// Takes filters and creates a string based on them that can be used in the URL for the JoinCreate page, to load those filters instantly.
    /// </summary>
    public string ConvertFiltersToUrlString(
        ICollection<int> cinemaIds = null!,
        ICollection<int> movieIds = null!,
        ICollection<int> genreIds = null!,
        DateTime fromDate = default,
        DateTime toDate = default
    );

    /// <summary>
    /// This will determine the selected movies, cinemas, genres, start date and end date used for the filters,
    /// based on the filter string taken from the url.
    /// </summary>
    /// <returns>
    /// A tuple with the following: <br/>
    /// - selected cinema ids <br/>
    /// - selected movie ids <br/>
    /// - selected genre ids <br/>
    /// - start date or default if none found <br/>
    /// - end date or default if none found
    /// </returns>
    public (
        ISet<int> selectedCinemas,
        ISet<int> selectedMovies,
        ISet<int> selectedGenres,
        DateTime startDate,
        DateTime endDate,
        SortOption sortOption
    ) GetFiltersFromUrlFilterString(string filterString);
}
