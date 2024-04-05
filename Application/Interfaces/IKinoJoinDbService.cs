using System.Linq.Expressions;

namespace Application.Interfaces;

/**
 * <summary>
 * Handles the interaction with the database for join events, and other entities.
 * </summary>
 **/
public interface IKinoJoinDbService
{
    /**
     * <summary>
     * If the database already has a join event with the same id,
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
     * </returns>
     */
    Task<JoinEvent?> GetJoinEventAsync(int id);

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
     * </returns>
     */
    Task<List<JoinEvent>> GetAllJoinEventsAsync(Expression<Func<JoinEvent, bool>>? filter = null);

    /// <summary>
    ///  Deletes the participant with the given id for the join event with the given id, if it exists.
    /// </summary>
    Task MakeParticipantNotExistAsync(int joinEventId, int participantId);

    Task<ICollection<Cinema>> GetAllCinemasAsync();
    Task<ICollection<Movie>> GetAllMoviesAsync();
    Task<ICollection<Genre>> GetAllGenresAsync();
}
