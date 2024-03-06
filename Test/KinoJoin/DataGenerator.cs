using Bogus;
using Domain.Entities;

namespace Test.KinoJoin;

public class DataGenerator
{
    private readonly Faker<Cinema> _cinemaGenerator;
    private readonly Faker<Movie> _movieGenerator;
    private readonly Faker<Host> _hostGenerator;
    private readonly Faker<Showtime> _showtimeGenerator;
    private readonly Faker<Participant> _participantGenerator;
    private readonly Faker<SelectOption> _selectOptionGenerator;
    private readonly Faker<Playtime> _playtimeGenerator;
    private readonly Faker<VersionTag> _versionTagGenerator;
    private readonly Faker<Room> _roomGenerator;

    public readonly Faker<JoinEvent> JoinEventGenerator;

    public DataGenerator()
    {
        _playtimeGenerator = new Faker<Playtime>()
            .RuleFor(p => p.Id, (f, p) => f.IndexFaker + 1)
            .RuleFor(p => p.StartTime, f => f.Date.Future());

        _versionTagGenerator = new Faker<VersionTag>()
            .RuleFor(v => v.Id, (f, v) => f.IndexFaker + 1)
            .RuleFor(v => v.Type, f => f.Lorem.Word());

        _roomGenerator = new Faker<Room>()
            .RuleFor(r => r.Id, (f, r) => f.IndexFaker + 1)
            .RuleFor(r => r.Name, f => f.Lorem.Word());

        _cinemaGenerator = new Faker<Cinema>()
            .RuleFor(c => c.Id, (f, c) => f.IndexFaker + 1)
            .RuleFor(c => c.Name, f => f.Company.CompanyName());

        _movieGenerator = new Faker<Movie>()
            .RuleFor(m => m.Id, (f, m) => f.IndexFaker + 1)
            .RuleFor(m => m.Title, f => f.Lorem.Sentence())
            .RuleFor(m => m.ImageUrl, f => f.Image.PicsumUrl())
            .RuleFor(m => m.KinoURL, f => f.Internet.Url())
            .RuleFor(m => m.Duration, f => f.Random.Int(60, 180))
            .RuleFor(m => m.PremiereDate, f => f.Date.Past().ToString())
            .RuleFor(m => m.AgeRating, f => f.Lorem.Word());

        _hostGenerator = new Faker<Host>()
            .RuleFor(h => h.AuthId, f => f.Random.Uuid().ToString())
            .RuleFor(h => h.Email, f => f.Internet.Email())
            .RuleFor(h => h.Username, f => f.Internet.UserName());

        _showtimeGenerator = new Faker<Showtime>()
            .RuleFor(s => s.Id, (f, s) => f.IndexFaker + 1)
            .RuleFor(s => s.Movie, f => f.PickRandom(_movieGenerator.Generate()))
            .RuleFor(s => s.Cinema, f => f.PickRandom(_cinemaGenerator.Generate()))
            .RuleFor(s => s.Playtime, f => f.PickRandom(_playtimeGenerator.Generate()))
            .RuleFor(s => s.VersionTag, f => f.PickRandom(_versionTagGenerator.Generate()))
            .RuleFor(s => s.Room, f => f.PickRandom(_roomGenerator.Generate()));

        _participantGenerator = new Faker<Participant>()
            .RuleFor(p => p.Id, (f, p) => f.IndexFaker + 1)
            .RuleFor(p => p.AuthId, f => f.Random.Uuid().ToString())
            .RuleFor(p => p.Nickname, f => f.Internet.UserName())
            .RuleFor(p => p.Email, f => f.Internet.Email())
            .RuleFor(p => p.Note, f => f.Lorem.Sentence());

        _selectOptionGenerator = new Faker<SelectOption>()
            .RuleFor(o => o.Id, (f, o) => f.IndexFaker + 1)
            .RuleFor(o => o.VoteOption, f => f.Lorem.Word())
            .RuleFor(o => o.Color, f => f.Commerce.Color());

        JoinEventGenerator = new Faker<JoinEvent>().CustomInstantiator(f =>
        {
            var JoinEventId = f.IndexFaker + 1;

            var participants = _participantGenerator.Generate(f.Random.Int(0, 5));

            var joinEvent = new JoinEvent
            {
                Id = JoinEventId,
                HostId = f.Random.Uuid().ToString(),
                Title = f.Lorem.Sentence(),
                Description = f.Lorem.Paragraph(),
                Showtimes = _showtimeGenerator.Generate(f.Random.Int(1, 5)),
                Participants = participants,
                SelectOptions = _selectOptionGenerator.Generate(f.Random.Int(1, 5)),
                Deadline = f.Date.Future(),
                Host = _hostGenerator.Generate(),
            };

            if (joinEvent.Showtimes.Any())
            {
                joinEvent.ChosenShowtimeId = f.PickRandom(joinEvent.Showtimes).Id;
            }

            // After the event has been created, insert some random participants who have made random votes
            foreach (var participant in joinEvent.Participants)
            {
                participant.JoinEventId = JoinEventId;
                participant.VotedFor = new List<ParticipantVote>();
                var numberOfVotes = f.Random.Int(0, joinEvent.Showtimes.Count);
                var remainingShowtimes = joinEvent.Showtimes.Select(s => s.Id).ToList();
                for (int i = 0; i < numberOfVotes; i++)
                {
                    var chosenShowtime = f.PickRandom(remainingShowtimes);
                    participant.VotedFor.Add(
                        new ParticipantVote
                        {
                            ParticipantId = participant.Id,
                            ShowtimeId = chosenShowtime,
                            VoteIndex = f.Random.Int(0, joinEvent.Showtimes.Count - 1)
                        }
                    );
                    remainingShowtimes.Remove(chosenShowtime);
                }
            }

            if (joinEvent.Showtimes.Any())
            {
                joinEvent.ChosenShowtimeId = f.PickRandom(joinEvent.Showtimes).Id;
            }

            return joinEvent;
        });
    }
}
