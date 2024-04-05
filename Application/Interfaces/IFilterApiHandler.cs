namespace Application.Interfaces;

/// <summary>
/// The main idea of this interface is to handle the filters namely, cinemas, movies, genres and dates.
/// </summary>
public interface IFilterApiHandler
{
    /// <summary>
    /// Fetches matching showtimes, and provides a list of movies that don't have any matching showtimes, but are included in the movie filters.
    /// </summary>
    Task<(List<Showtime> showtimes, List<Movie> moviesWithoutShowtimes)> GetShowtimesFromFilters(
        ICollection<int> cinemaIds,
        ICollection<int> movieIds,
        ICollection<int> genreIds,
        DateTime fromDate,
        DateTime toDate
    );

    /// <summary>
    /// Takes filters and creates a string based on them that can be used in the URL for the JoinCreate page, to load those filters instantly.
    /// </summary>
    public string ConvertFiltersToUrlString(
        ICollection<int> cinemaIds,
        ICollection<int> movieIds,
        ICollection<int> genreIds,
        DateTime fromDate,
        DateTime toDate
    );
}
