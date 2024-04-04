namespace Application.Interfaces;

public interface IFilterApiHandler
{
    //TODO På den ene har alle default med null og nullable, på den anden er det ikke. Gør ensartet
    Task<(List<Showtime> showtimes, List<Movie> moviesWithoutShowtimes)> GetShowtimesFromFilters(
        ICollection<int>? cinemaIds = null,
        ICollection<int>? movieIds = null,
        ICollection<int>? genreIds = null,
        DateTime? fromDate = null,
        DateTime? toDate = null
    );

    public string ConvertFiltersToUrlString(
        ICollection<int> cinemaIds,
        ICollection<int> movieIds,
        ICollection<int> genreIds,
        DateTime fromDate,
        DateTime toDate
    );
}
