using System.Linq.Expressions;

namespace Application.Interfaces;

/**
 * <summary>
 * Handles the interaction with the database for join events.
 * </summary>
 **/
public interface IKinoJoinDbService
{
    /**
     * <summary>
     * If the database already has a join event with the same id as the dto,
     * it will update that entry, otherwise it will insert a new entry.
     * </summary>
     *
     * <returns> The id of the upserted join event</returns>
     */
    Task<int> UpsertJoinEventAsync(JoinEvent updatedJoinEvent);

    /**
     * <summary>
     * Gets a join event from the database by its id.
     * </summary>
     *
     * <returns>
     * The join event with the given id, or null if no such join event exists.
     * A non-dto is returned to include all nested data.
     * </returns>
     */
    Task<JoinEvent?> GetAsync(int id);

    /**
     * <summary>
     * Gets all join events from the database.
     * </summary>
     *
     * <param name="filter">
     * An optional filter to apply to the join events.
     * </param>
     *
     * <returns>
     * A list of all join events in the database.
     * Non-dtos are returned to include all nested data.
     * </returns>
     */
    Task<List<JoinEvent>> GetAllAsync(Expression<Func<JoinEvent, bool>>? filter = null);

    //Delete participant
    Task MakeParticipantNotExistAsync(int joinEventId, int participantId);

    Task<ICollection<Cinema>> GetAllCinemas();
    Task<ICollection<Movie>> GetAllMovies();
    Task<ICollection<Genre>> GetAllGenres();
}
