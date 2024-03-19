namespace Application.Interfaces;

public interface IKinoJoinService
{
    Task<ICollection<Cinema>> GetAllCinemas(Func<Cinema, bool>? filter = null);
    Task<ICollection<Movie>> GetAllMovies(Func<Movie, bool>? filter = null);
    Task<ICollection<Genre>> GetAllGenres(Func<Genre, bool>? filter = null);
}
