namespace Infrastructure.Persistence;

public class KinoContext : DbContext
{
    //This is data used by KinoJoin
    public DbSet<Host> Hosts { get; set; } = default!;
    public DbSet<Participant> Participants { get; set; } = default!;
    public DbSet<JoinEvent> JoinEvents { get; set; } = default!;
    public DbSet<ParticipantVote> ParticipantVotes { get; set; } = default!;
    public DbSet<SelectOption> SelectOptions { get; set; } = default!;

    //This is basedata comming from api.Kino.dk
    public DbSet<Movie> Movies { get; set; } = default!;
    public DbSet<Showtime> Showtimes { get; set; } = default!;
    public DbSet<Playtime> Playtimes { get; set; } = default!;
    public DbSet<VersionTag> Versions { get; set; } = default!;
    public DbSet<Cinema> Cinemas { get; set; } = default!;
    public DbSet<Room> Rooms { get; set; } = default!;
    public DbSet<Genre> Genres { get; set; } = default!;
    public DbSet<AgeRating> AgeRatings { get; set; } = default!;

    public KinoContext(DbContextOptions<KinoContext> options)
        : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfiguration(new JoinEventConfiguration());
        modelBuilder.ApplyConfiguration(new ParticipantVoteConfiguration());
        modelBuilder.ApplyConfiguration(new ParticipantConfiguration());
        modelBuilder.ApplyConfiguration(new ShowtimeConfiguration());
        modelBuilder.ApplyConfiguration(new MovieConfiguration());
        modelBuilder.ApplyConfiguration(new PlaytimeConfiguration());
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.EnableSensitiveDataLogging();
        optionsBuilder.EnableDetailedErrors();
    }
}
