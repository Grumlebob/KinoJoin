namespace Infrastructure.Services;

public class JoinEventService(KinoContext context) : IJoinEventService
{
    public async Task<int> PutAsync(JoinEvent joinEvent)
    {
        //make sure all entities exist in database 
        await context.Hosts.Upsert(joinEvent.Host).RunAsync();

        await context.SelectOptions.UpsertRange(joinEvent.SelectOptions).On(s => new { s.VoteOption, s.Color })
            .RunAsync();

        var cinemas = joinEvent.Showtimes.Select(st => st.Cinema).DistinctBy(c => c.Id);
        await context.Cinemas.UpsertRange(cinemas).RunAsync();

        var movies = joinEvent.Showtimes.Select(st => st.Movie).DistinctBy(m => m.Id);
        await context.Movies.UpsertRange(movies).RunAsync();

        var playtimes = joinEvent.Showtimes.Select(st => st.Playtime).DistinctBy(p => p.StartTime);
        await context.Playtimes.UpsertRange(playtimes).On(p => p.StartTime).RunAsync();

        var versions = joinEvent.Showtimes.Select(st => st.VersionTag).DistinctBy(v => v.Type);
        await context.Versions.UpsertRange(versions).On(v => v.Type).RunAsync();

        var rooms = joinEvent.Showtimes.Select(st => st.Room).DistinctBy(r => r.Id);
        await context.Rooms.UpsertRange(rooms).RunAsync();

        await context.SaveChangesAsync();

        foreach (var showtime in joinEvent.Showtimes)
        {
            showtime.CinemaId = showtime.Cinema.Id;
            showtime.MovieId = showtime.Movie.Id;
            showtime.RoomId = showtime.Room.Id;
            var existingPlaytime = await context.Playtimes.FirstAsync(p => p.StartTime == showtime.Playtime.StartTime);
            var existingVersionTag = await context.Versions.FirstAsync(v => v.Type == showtime.VersionTag.Type);
            showtime.PlaytimeId = existingPlaytime.Id;
            showtime.VersionTagId = existingVersionTag.Id;
        }

        await context.Showtimes.UpsertRange(joinEvent.Showtimes).RunAsync();
        await context.SaveChangesAsync();
        
        // Handle Host
        var existingHost = await context.Hosts.FindAsync(joinEvent.Host.AuthId);
        if (existingHost != null)
        {
            context.Hosts.Attach(existingHost);
            joinEvent.Host = existingHost;
        }
        else
        {
            context.Hosts.Add(joinEvent.Host);
        }

        await context.SaveChangesAsync();

        //Get id of new host
        joinEvent.HostId = joinEvent.Host.AuthId;

        //Add joinEvent with existing entities
        var showtimeIds = joinEvent.Showtimes.Select(s => s.Id);
        var voteOptions = joinEvent.SelectOptions.Select(s => s.VoteOption);
        var colorOptions = joinEvent.SelectOptions.Select(s => s.Color);

        var newJoinEvent = new JoinEvent
        {
            Title = joinEvent.Title,
            Description = joinEvent.Description,
            HostId = joinEvent.Host.AuthId,
            Deadline = joinEvent.Deadline,
            Participants = joinEvent.Participants,
            Showtimes = context.Showtimes.Where(s => showtimeIds.Contains(s.Id)).ToList(),
            SelectOptions = context.SelectOptions.Where(s => voteOptions.Contains(s.VoteOption) && colorOptions.Contains(s.Color))
                .ToList(),
            ChosenShowtimeId = joinEvent.ChosenShowtimeId
        };

        var newlyAddedJoinEvent = await context.JoinEvents.AddAsync(newJoinEvent);
        await context.SaveChangesAsync();

        return newlyAddedJoinEvent.Entity.Id;
    }
}

public class UpsertJoinEventDto
{
    public int? Id { get; set; } //If null, create new JoinEvent, else update existing
    public string Title { get; set; }
    public string Description { get; set; }
    public Host Host { get; set; } //Ikke nullable, rigtige version skal ikke understøtte ukendt vært
    public List<UpsertShowtimeDto> Showtimes { get; set; } = new();
    public int? ChosenShowtimeId { get; set; } //Nullable, on create it isn't set, on filling it is set.
    public DateTime Deadline { get; set; }
    
    public static UpsertJoinEventDto FromModelToUpsertDto(JoinEvent joinEvent)
    {
        if (joinEvent == null)
        {
            return null;
        }

        var upsertDto = new UpsertJoinEventDto
        {
            Id = joinEvent.Id,
            Title = joinEvent.Title,
            Description = joinEvent.Description,
            Host = joinEvent.Host, // Assuming Host class is the same in both models
            ChosenShowtimeId = joinEvent.ChosenShowtimeId,
            Deadline = joinEvent.Deadline,
            Showtimes = joinEvent.Showtimes.Select(st => new UpsertShowtimeDto
            {
                Id = st.Id,
                MovieId = st.MovieId,
                CinemaId = st.CinemaId,
                PlaytimeId = st.PlaytimeId,
                VersionTagId = st.VersionTagId,
                RoomId = st.RoomId
            }).ToList()
        };

        return upsertDto;
    }
    
}

public class UpsertShowtimeDto
{
    public int Id { get; set; }
    public int MovieId { get; set; }
    public int CinemaId { get; set; }
    public int PlaytimeId { get; set; }
    public int VersionTagId { get; set; }
    public int RoomId { get; set; }
}

public class UpsertParticipantDto
{
    public int? Id { get; set; } //If null, create new JoinEvent, else update existing
    public string? AuthId { get; set; }
    public int JoinEventId { get; set; }
    public string Nickname { get; set; }
    public string? Email { get; set; }
    public string? Note { get; set; }
    public List<ParticipantVote> VotedFor { get; set; } = new();
}

public class UpsertParticipantVoteDto
{
    public int ShowtimeId { get; set; }
    public int VoteIndex { get; set; }
}

public class UpsertSelectOptionDto
{
    public int? Id { get; set; }  //If null, create new JoinEvent, else update existing
    public string VoteOption { get; set; }
    public string Color { get; set; }
}

