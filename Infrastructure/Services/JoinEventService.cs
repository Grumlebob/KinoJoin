namespace Infrastructure.Services;

public class JoinEventService(KinoContext context) : IJoinEventService
{
    public async Task<int> PutAsync(JoinEvent joinEvent)
    {
        foreach (var showtime in joinEvent.Showtimes!)
        {
            var cinemas = joinEvent.Showtimes.Select(st => st.Cinema).DistinctBy(c => c.Id);
            await context.Cinemas.UpsertRange(cinemas).RunAsync();


            await context.Playtimes.Upsert(showtime.Playtime).On(p => p.StartTime).RunAsync();

            /* Handle Playtime
            var existingPlaytime = await context.Playtimes.FirstOrDefaultAsync(p =>
                p.StartTime == showtime.Playtime.StartTime
            );
            if (existingPlaytime != null)
            {
                context.Playtimes.Attach(existingPlaytime);
                showtime.Playtime = existingPlaytime;
            }
            else
            {
                context.Playtimes.Add(showtime.Playtime);
            }*/

            await context.Versions.Upsert(showtime.VersionTag).On(v => v.Type).RunAsync();

            // Handle VersionTag
            /*
            var existingVersionTag = await context.Versions.FirstOrDefaultAsync(v =>
                v.Type == showtime.VersionTag.Type
            );
            if (existingVersionTag != null)
            {
                Console.WriteLine("Existing version tag: " + existingVersionTag.Type);
                context.Versions.Attach(existingVersionTag);
                showtime.VersionTag = existingVersionTag;
            }
            else
            {
                context.Versions.Add(showtime.VersionTag);
            }*/

            await context.Rooms.Upsert(showtime.Room).RunAsync();

            // Handle Sal
            /*
            var existingSal = await context.Rooms.FindAsync(showtime.Room.Id);
            if (existingSal != null)
            {
                context.Rooms.Attach(existingSal);
                showtime.Room = existingSal;
            }
            else
            {
                context.Rooms.Add(showtime.Room);
            }
            */

            await context.SaveChangesAsync();
        }

        //Handle movies Same way as cinemas

        var movies = joinEvent.Showtimes.Select(st => st.Movie).DistinctBy(m => m.Id);
        await context.Movies.UpsertRange(movies).RunAsync();


        await context.SaveChangesAsync();

        context.Showtimes.UpsertRange(joinEvent.Showtimes);

        await context.SaveChangesAsync();

        // Handle Host
        await context.Hosts.Upsert(joinEvent.Host).RunAsync();

        await context.SaveChangesAsync();

        //handle selectOptions
        await context.SelectOptions.UpsertRange(joinEvent.SelectOptions).On(s => new { s.VoteOption, s.Color }).RunAsync();

        await context.SaveChangesAsync();

        //Get id of new host
        joinEvent.HostId = joinEvent.Host.AuthId;
    
        var joinEventEntry = context.Entry(joinEvent);
        await joinEventEntry.Collection(j => j.Showtimes).LoadAsync();
        joinEventEntry.State = EntityState.Detached;
        var showtimesEntity = context.
        
        // Handle JoinEvent
        //var newJoinEventId = 0;
        if (joinEvent.Id == 0) //New
        {
            await context.JoinEvents.AddAsync(joinEvent);
        }
        else
        {
            context.JoinEvents.Update(joinEvent);
        }

        await context.SaveChangesAsync();
        /*
        if (existingJoinEvent != null)
        {
            existingJoinEvent.Title = joinEvent.Title;
            existingJoinEvent.Description = joinEvent.Description;
            existingJoinEvent.Deadline = joinEvent.Deadline;
            existingJoinEvent.ChosenShowtimeId = joinEvent.ChosenShowtimeId;
            newJoinEventId = joinEvent.Id;
            await context.SaveChangesAsync();
        }
        else
        {
            var ShowtimesToAttach = new List<Showtime>();
            foreach (var showtime in joinEvent.Showtimes)
            {
                var existingShowtime = await context.Showtimes.FindAsync(showtime.Id);
                if (existingShowtime != null)
                {
                    ShowtimesToAttach.Add(existingShowtime);
                }
                else
                {
                    ShowtimesToAttach.Add(showtime);
                }
            }

            var newJoinEvent = new JoinEvent
            {
                Id = joinEvent.Id,
                Title = joinEvent.Title,
                Description = joinEvent.Description,
                Deadline = joinEvent.Deadline,
                HostId = joinEvent.Host.AuthId,
                Showtimes = ShowtimesToAttach,
                ChosenShowtimeId = joinEvent.ChosenShowtimeId,
                SelectOptions = []
            };

            context.JoinEvents.Add(newJoinEvent);
            await context.SaveChangesAsync();
            newJoinEventId = newJoinEvent.Id;
        }
        
        //Get recently added joinEvent
        var recentlyAddedJoinEvent = await context
            .JoinEvents.Include(e => e.Showtimes)
            .FirstOrDefaultAsync(e => e.Id == newJoinEventId);
        */
        //Confirm attributes
        /*Console.WriteLine("JoinEvent: " + recentlyAddedJoinEvent.Id);
        Console.WriteLine("Title: " + recentlyAddedJoinEvent.Title);
        Console.WriteLine(
            "movie of first showtime: " + recentlyAddedJoinEvent.Showtimes.FirstOrDefault().Movie.Title);
            */

        return joinEvent.Id;
    }
}