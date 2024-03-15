namespace Application.Interfaces;

public interface IKinoDkService
{
    Task<(List<Showtime> showtimes, List<Movie> moviesWithoutShowtimes)> GetShowtimesFromFilters(List<int>? cinemaIds = null, List<int>? movieIds = null,
        List<int>? genreIds = null, DateTime? fromDate = null, DateTime? toDate = null);
}