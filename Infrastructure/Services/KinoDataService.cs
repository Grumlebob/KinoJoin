namespace Infrastructure.Services;

public class KinoDataService(KinoContext context) : IKinoDataService
{

    public async Task<ICollection<Cinema>> GetAllCinemas(Func<Cinema, bool>? filter = null)
    {
        var query = context.Cinemas.AsNoTracking();
        if (filter != null)
        {
            query = query.AsEnumerable().Where(filter).AsQueryable();
        }

        return await query.ToListAsync();
    }

    public async Task<ICollection<Movie>> GetAllMovies(Func<Movie, bool>? filter = null)
    {
        var query = context.Movies.AsNoTracking();
        if (filter != null)
        {
            query = query.AsEnumerable().Where(filter).AsQueryable();
        }

        return await query.ToListAsync();
    }
    
    public async Task<ICollection<Genre>> GetAllGenres(Func<Genre, bool>? filter = null)
    {
        var query = context.Genres.AsNoTracking();
        if (filter != null)
        {
            query = query.AsEnumerable().Where(filter).AsQueryable();
        }

        return await query.ToListAsync();
    }
    
}