﻿namespace Infrastructure.Database;

public class KinoContext : DbContext
{
    //For KinoJoin
    public DbSet<Host> Hosts { get; set; }
    public DbSet<Participant> Participants { get; set; }
    public DbSet<JoinEvent> JoinEvents { get; set; }
    public DbSet<ParticipantVote> ParticipantVotes { get; set; }

    //For basedata from Kino
    public DbSet<Movie> Movies { get; set; }
    public DbSet<Showtime> Showtimes { get; set; }
    public DbSet<Playtime> Playtimes { get; set; }
    public DbSet<VersionTag> Versions { get; set; }
    public DbSet<Cinema> Cinemas { get; set; }
    public DbSet<Room> Rooms { get; set; }
    public DbSet<Genre> Genres { get; set; }

    public DbSet<SelectOption> SelectOptions { get; set; }

    public KinoContext(DbContextOptions<KinoContext> options)
        : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        //Super keys
        modelBuilder
            .Entity<ParticipantVote>()
            .HasKey(pv => new { pv.ParticipantId, pv.ShowtimeId });

        //Many to Many relations
        modelBuilder.Entity<JoinEvent>()
            .HasMany(je => je.Showtimes)
            .WithMany(s => s.JoinEvents)
            .UsingEntity(
                "JoinEventShowtime",
                l => l.HasOne(typeof(JoinEvent)).WithMany().HasForeignKey("JoinEventsId").HasPrincipalKey(nameof(JoinEvent.Id)),
                r => r.HasOne(typeof(Showtime)).WithMany().HasForeignKey("ShowtimesId").HasPrincipalKey(nameof(Showtime.Id)),
                j => j.HasKey("JoinEventsId", "ShowtimesId"));

        
        modelBuilder
            .Entity<JoinEvent>()
            .HasMany(je => je.SelectOptions)
            .WithMany(so => so.JoinEvents)
            .UsingEntity(
                "JoinEventSelectOption",
                l => l.HasOne(typeof(JoinEvent)).WithMany().HasForeignKey("JoinEventsId").HasPrincipalKey(nameof(JoinEvent.Id)),
                r => r.HasOne(typeof(SelectOption)).WithMany().HasForeignKey("SelectOptionsId").HasPrincipalKey(nameof(SelectOption.Id)),
                j => j.HasKey("JoinEventsId", "SelectOptionsId"));;

        // Many to one relations
        modelBuilder.Entity<JoinEvent>()
            .HasMany(je => je.Participants)
            .WithOne().HasForeignKey(p => p.JoinEventId);
        
        modelBuilder
            .Entity<Participant>()
            .HasMany(p => p.VotedFor)
            .WithOne()
            .HasForeignKey(pv => pv.ParticipantId);

        modelBuilder
            .Entity<Showtime>()
            .HasMany<ParticipantVote>()
            .WithOne()
            .HasForeignKey(v => v.ShowtimeId);

        //modelBuilder.Entity<ParticipantVote>().Property(pv => pv.VoteIndex);

        // Call the base method to ensure any configuration from the base class is applied
        base.OnModelCreating(modelBuilder);
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder) { }
}
