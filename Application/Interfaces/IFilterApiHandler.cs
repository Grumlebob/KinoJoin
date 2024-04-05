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
        DateTime toDate = default
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
}
