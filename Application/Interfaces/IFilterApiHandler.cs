namespace Application.Interfaces;

public interface IFilterApiHandler
{
    Task<(List<Showtime> showtimes, List<Movie> moviesWithoutShowtimes)> GetShowtimesFromFilters(
        ICollection<int>? cinemaIds = null,
        ICollection<int>? movieIds = null,
        ICollection<int>? genreIds = null,
        DateTime? fromDate = null,
        DateTime? toDate = null
    );

    public string GetUrlFilterString(
        ICollection<int> cinemaIds,
        ICollection<int> movieIds,
        ICollection<int> genreIds,
        DateTime fromDate,
        DateTime toDate
    );
}
