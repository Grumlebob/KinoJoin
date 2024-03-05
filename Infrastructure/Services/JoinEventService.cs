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