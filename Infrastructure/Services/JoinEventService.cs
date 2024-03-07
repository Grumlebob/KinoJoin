using System.Text;

namespace Infrastructure.Services;

public class JoinEventService(KinoContext context) : IJoinEventService
{
    public async Task<int> PutAsync(UpsertJoinEventDto joinEventDto)
    {
        var joinEventWithNavProps = UpsertJoinEventDto.FromUpsertDtoToModel(joinEventDto);

        //make sure Host,selectOptions,Playtimes and rooms entities exist in database
        
        // 1. Host Upsert
        var existingHost = await context.Hosts.FindAsync(joinEventWithNavProps.Host.AuthId);
        if (existingHost == null)
        {
            await context.Hosts.AddAsync(joinEventWithNavProps.Host);
        }

        // 2. SelectOptions Upsert
        await context
            .SelectOptions.UpsertRange(joinEventWithNavProps.SelectOptions)
            .On(s => new { s.VoteOption, s.Color })
            .RunAsync();
        
        // 3. Playtimes Upsert
        var playtimeStartTimes = joinEventWithNavProps.Showtimes.Select(st => st.Playtime.StartTime).Distinct();
        var existingPlaytimes = await context.Playtimes
            .Where(p => playtimeStartTimes.Contains(p.StartTime))
            .ToListAsync();

        var newPlaytimes = joinEventWithNavProps.Showtimes
            .Select(st => st.Playtime)
            .DistinctBy(p => p.StartTime)
            .Except(existingPlaytimes)
            .ToList();

        if (newPlaytimes.Any())
        {
            await context.Playtimes.UpsertRange(newPlaytimes).On(p => p.StartTime).RunAsync();
        }
        
        // 4. Rooms Upsert
        var roomIds = joinEventWithNavProps.Showtimes.Select(st => st.Room.Id).Distinct();
        var existingRooms = await context.Rooms
            .Where(r => roomIds.Contains(r.Id))
            .ToListAsync();

        var newRooms = joinEventWithNavProps.Showtimes
            .Select(st => st.Room)
            .DistinctBy(r => r.Id)
            .Except(existingRooms)
            .ToList();

        if (newRooms.Any())
        {
            await context.Rooms.UpsertRange(newRooms).RunAsync();
        }

        await context.SaveChangesAsync();

        //Try to add JoinEvent using preseeded data in database
        try
        {
            await AttachOnlyOnePlaytimeAndVersionToEfCoreBatched(joinEventWithNavProps.Showtimes);
            await context.Showtimes.UpsertRange(joinEventWithNavProps.Showtimes).RunAsync();
            await context.SaveChangesAsync();
        }
        catch (Exception _) //If not all data was found in the preseeded database, add it and try again
        {
            // 1. Movies Upsert
            var movieIds = joinEventWithNavProps.Showtimes.Select(st => st.Movie.Id).Distinct();
            var existingMovies = await context.Movies
                .Where(m => movieIds.Contains(m.Id))
                .ToListAsync();

            var newMovies = joinEventWithNavProps.Showtimes
                .Select(st => st.Movie)
                .DistinctBy(m => m.Id)
                .Except(existingMovies)
                .ToList();

            if (newMovies.Any())
            {
                await context.Movies.UpsertRange(newMovies).RunAsync();
            }

            // 2. Cinemas Upsert
            var cinemaIds = joinEventWithNavProps.Showtimes.Select(st => st.Cinema.Id).Distinct();
            var existingCinemas = await context.Cinemas
                .Where(c => cinemaIds.Contains(c.Id))
                .ToListAsync();

            var newCinemas = joinEventWithNavProps.Showtimes
                .Select(st => st.Cinema)
                .DistinctBy(c => c.Id)
                .Except(existingCinemas)
                .ToList();

            if (newCinemas.Any())
            {
                await context.Cinemas.UpsertRange(newCinemas).RunAsync();
            }

            //3. Versions Upsert
            var versionTypes = joinEventWithNavProps.Showtimes.Select(st => st.VersionTag.Type).Distinct();
            var existingVersions = await context.Versions
                .Where(v => versionTypes.Contains(v.Type))
                .ToListAsync();
            var newVersions = joinEventWithNavProps.Showtimes
                .Select(st => st.VersionTag)
                .DistinctBy(v => v.Type)
                .Except(existingVersions)
                .ToList();
            if (newVersions.Any())
            {
                await context.Versions.UpsertRange(newVersions).On(v => v.Type).RunAsync();
            }
            
            await AttachOnlyOnePlaytimeAndVersionToEfCoreBatched(joinEventWithNavProps.Showtimes);
            await context.Showtimes.UpsertRange(joinEventWithNavProps.Showtimes).RunAsync();
            await context.SaveChangesAsync();
        }

        //Setting up EF core to only have one instance of each entity
        //Avoid creating a new host because relation will be sat from ID
        joinEventWithNavProps.Host = null!;
        //As mulitple showtimes can reference the same movie,cinema, etc, we must only attach it once in ef core
        joinEventWithNavProps.Showtimes = context
            .Showtimes.Where(s => joinEventDto.Showtimes.Select(s => s.Id).Contains(s.Id))
            .ToList();
        //As multiple selectOptions can reference the same voteOption and color, we must only attach it once in ef core
        joinEventWithNavProps.SelectOptions = context
            .SelectOptions.Where(s =>
                joinEventDto.SelectOptions.Select(s => s.VoteOption).Contains(s.VoteOption)
                && joinEventDto.SelectOptions.Select(s => s.Color).Contains(s.Color)
            )
            .ToList();

        var newlyAddedJoinEvent =
            joinEventDto.Id == null //Create or Update based on if ID is set
                ? await context.JoinEvents.AddAsync(joinEventWithNavProps)
                : context.JoinEvents.Update(joinEventWithNavProps);

        await context.SaveChangesAsync();
        return newlyAddedJoinEvent.Entity.Id;
    }

    private async Task AttachOnlyOnePlaytimeAndVersionToEfCoreBatched(
        IEnumerable<Showtime> showtimes
    )
    {
        try
        {
            // Retrieve all distinct StartTimes and Types from the showtimes.
            var showtimesList = showtimes.ToList();
            var distinctPlaytimes = showtimesList.Select(st => st.Playtime.StartTime).Distinct();
            var distinctVersionTypes = showtimesList.Select(st => st.VersionTag.Type).Distinct();

            // Retrieve all matching Playtimes and VersionTags from the database.
            var playtimes = await context
                .Playtimes.Where(p => distinctPlaytimes.Contains(p.StartTime))
                .ToListAsync();
            var versions = await context
                .Versions.Where(v => distinctVersionTypes.Contains(v.Type))
                .ToListAsync();

            //Convert to Dict for O(1) lookup
            var versionDict = versions.ToDictionary(v => v.Type, v => v.Id);
            var playtimeDict = playtimes
                .Distinct()
                .ToDictionary(p => p.StartTime.ToOADate(), p => p.Id);

            foreach (var showtime in showtimesList)
            {
                var lookupKey = showtime.Playtime.StartTime.ToOADate();
                if (!versionDict.TryGetValue(showtime.VersionTag.Type, out var versionTagId))
                {
                    throw new InvalidOperationException(
                        $"Version not found for Type: {showtime.VersionTag.Type}"
                    );
                }
                if (!playtimeDict.TryGetValue(lookupKey, out var playtimeId))
                {
                    throw new InvalidOperationException(
                        $"Playtime not found for: {lookupKey} in {String.Join(", ", playtimeDict.Keys.Select(k => k.ToString()))}"
                    );
                }
                showtime.PlaytimeId = playtimeId;
                showtime.VersionTagId = versionTagId;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine(
                $"Error in AttachOnlyOnePlaytimeAndVersionToEfCoreBatched: {ex.Message}"
            );
            throw;
        }
    }
}
