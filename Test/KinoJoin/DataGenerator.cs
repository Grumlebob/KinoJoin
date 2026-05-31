using System.Globalization;
using Bogus;
using Domain;

namespace Test.KinoJoin;

public class DataGenerator
{
    public readonly Faker<JoinEvent> JoinEventGenerator;

    public DataGenerator()
    {
        Faker<Playtime> playtimeGenerator = new Faker<Playtime>().RuleFor(
            p => p.StartTime,
            f =>
            {
                var d = f.Date.Future();
                //remove precision down to seconds because ef core is not precise down to a few ticks differences
                return d.AddTicks(-(d.Ticks % TimeSpan.TicksPerSecond));
            }
        );

        Faker<VersionTag> versionTagGenerator = new Faker<VersionTag>().RuleFor(
            v => v.Type,
            f => f.Lorem.Word()
        );

        Faker<Room> roomGenerator = new Faker<Room>()
            .RuleFor(r => r.Id, (f, _) => f.IndexFaker + 1)
            .RuleFor(r => r.Name, f => f.Lorem.Word());

        Faker<Cinema> cinemaGenerator = new Faker<Cinema>()
            .RuleFor(c => c.Id, (f, _) => f.IndexFaker + 1)
            .RuleFor(c => c.Name, f => f.Company.CompanyName());

        Faker<AgeRating> ageRatingGenerator = new Faker<AgeRating>().RuleFor(
            a => a.Censorship,
            f => f.Lorem.Word()
        );

        Faker<Movie> movieGenerator = new Faker<Movie>()
            .RuleFor(m => m.Id, (f, _) => f.IndexFaker + 1)
            .RuleFor(m => m.Title, f => f.Lorem.Sentence())
            .RuleFor(m => m.ImageUrl, f => f.Image.PicsumUrl())
            .RuleFor(m => m.KinoUrl, f => f.Internet.Url())
            .RuleFor(m => m.DurationInMinutes, f => f.Random.Int(60, 180))
            .RuleFor(m => m.PremiereDate, f => f.Date.Past().ToString(CultureInfo.InvariantCulture))
            .RuleFor(m => m.AgeRating, _ => ageRatingGenerator.Generate());

        Faker<Host> hostGenerator = new Faker<Host>()
            .RuleFor(h => h.AuthId, f => f.Random.Uuid().ToString())
            .RuleFor(h => h.Email, f => f.Internet.Email())
            .RuleFor(h => h.Username, f => f.Internet.UserName());

        Faker<Showtime> showtimeGenerator = new Faker<Showtime>()
            .RuleFor(s => s.Id, (f, _) => f.IndexFaker + 1)
            .RuleFor(s => s.Movie, f => f.PickRandom(movieGenerator.Generate(5)))
            .RuleFor(s => s.Cinema, f => f.PickRandom(cinemaGenerator.Generate(5)))
            .RuleFor(s => s.Playtime, f => f.PickRandom(playtimeGenerator.Generate(5)))
            .RuleFor(s => s.VersionTag, f => f.PickRandom(versionTagGenerator.Generate(5)))
            .RuleFor(s => s.Room, f => f.PickRandom(roomGenerator.Generate(5)));

        Faker<Participant> participantGenerator = new Faker<Participant>()
            .RuleFor(p => p.AuthId, f => f.Random.Uuid().ToString())
            .RuleFor(p => p.Nickname, f => f.Internet.UserName())
            .RuleFor(p => p.Email, f => f.Internet.Email())
            .RuleFor(p => p.Note, f => f.Lorem.Sentence(3));

        Faker<SelectOption> selectOptionGenerator = new Faker<SelectOption>()
            .RuleFor(o => o.VoteOption, f => f.Lorem.Word())
            .RuleFor(o => o.Color, f => f.Commerce.Color());

        JoinEventGenerator = new Faker<JoinEvent>().CustomInstantiator(f =>
        {
            var participants = participantGenerator.Generate(f.Random.Int(0, 5));

            var joinEvent = new JoinEvent
            {
                HostId = f.Random.Uuid().ToString(),
                Title = f.Lorem.Sentence(3),
                Description = f.Lorem.Paragraph(),
                Showtimes = showtimeGenerator.Generate(f.Random.Int(5, 5)),
                Participants = participants,
                SelectOptions = selectOptionGenerator.Generate(f.Random.Int(2, 5)),
                Deadline = f.Date.Future(),
                Host = hostGenerator.Generate(),
            };

            joinEvent.DefaultSelectOptionId = joinEvent.SelectOptions.First().Id;
            joinEvent.DefaultSelectOption = joinEvent.SelectOptions.First();

            if (joinEvent.Showtimes.Any())
            {
                joinEvent.ChosenShowtimeId = f.PickRandom(joinEvent.Showtimes).Id;
            }

            // After the event has been created, insert some random participants who have made random votes
            foreach (var participant in joinEvent.Participants)
            {
                participant.JoinEventId = joinEvent.Id;
                participant.VotedFor = new List<ParticipantVote>();

                participant.VotedFor = joinEvent
                    .Showtimes.Select(s => new ParticipantVote
                    {
                        ParticipantId = participant.Id,
                        ShowtimeId = s.Id,
                        SelectedOption = f.PickRandom(joinEvent.SelectOptions)
                    })
                    .ToList();
            }

            if (joinEvent.Showtimes.Any())
            {
                joinEvent.ChosenShowtimeId = f.PickRandom(joinEvent.Showtimes).Id;
            }

            return joinEvent;
        });
    }
}
