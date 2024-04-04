namespace Application.Interfaces;

public interface IFilterApiHandler
{
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
