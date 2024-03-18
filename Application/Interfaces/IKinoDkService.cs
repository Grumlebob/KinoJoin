namespace Application.Interfaces;

public interface IKinoDkService
{
    Task<(List<Showtime> showtimes, List<Movie> moviesWithoutShowtimes)> GetShowtimesFromFilters(
        ICollection<int>? cinemaIds = null,
        ICollection<int>? movieIds = null,
        ICollection<int>? genreIds = null,
        DateTime? fromDate = null,
        DateTime? toDate = null
    );
}
