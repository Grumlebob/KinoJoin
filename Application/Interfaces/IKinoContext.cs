using Domain;

namespace Application.Interfaces;

public interface IKinoContext
{
    DbSet<Host> Hosts { get; set; }
    DbSet<Participant> Participants { get; set; }
    DbSet<JoinEvent> JoinEvents { get; set; }
    DbSet<ParticipantVote> ParticipantVotes { get; set; }
    DbSet<SelectOption> SelectOptions { get; set; }
    DbSet<Movie> Movies { get; set; }
    DbSet<Showtime> Showtimes { get; set; }
    DbSet<Playtime> Playtimes { get; set; }
    DbSet<VersionTag> Versions { get; set; }
    DbSet<Cinema> Cinemas { get; set; }
    DbSet<Room> Rooms { get; set; }
    DbSet<Genre> Genres { get; set; }
    DbSet<AgeRating> AgeRatings { get; set; }

    //Base DbContext members
    ChangeTracker ChangeTracker { get; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default(CancellationToken));
    EntityEntry<TEntity> Entry<TEntity>(TEntity entity)
        where TEntity : class;
}
