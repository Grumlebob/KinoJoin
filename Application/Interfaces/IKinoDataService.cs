namespace Infrastructure.Services;

public interface IKinoDataService
{
    Task<ICollection<Cinema>> GetAllCinemas(Func<Cinema, bool>? filter = null);
    Task<ICollection<Movie>> GetAllMovies(Func<Movie, bool>? filter = null);
    Task<ICollection<Genre>> GetAllGenres(Func<Genre, bool>? filter = null);
}