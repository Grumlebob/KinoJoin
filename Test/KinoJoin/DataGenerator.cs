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
    private readonly Faker<AgeRating> _ageRatingGenerator;

    public readonly Faker<JoinEvent> JoinEventGenerator;

    public DataGenerator()
    {
        _playtimeGenerator = new Faker<Playtime>().RuleFor(
            p => p.StartTime,
            f =>
            {
                var d = f.Date.Future();
                //remove precision down to seconds because ef core is not precise down to a few ticks differences
                return d.AddTicks(-(d.Ticks % TimeSpan.TicksPerSecond));
            }
        );

        _versionTagGenerator = new Faker<VersionTag>().RuleFor(v => v.Type, f => f.Lorem.Word());

        _roomGenerator = new Faker<Room>()
            .RuleFor(r => r.Id, (f, r) => f.IndexFaker + 1)
            .RuleFor(r => r.Name, f => f.Lorem.Word());

        _cinemaGenerator = new Faker<Cinema>()
            .RuleFor(c => c.Id, (f, c) => f.IndexFaker + 1)
            .RuleFor(c => c.Name, f => f.Company.CompanyName());

        _ageRatingGenerator = new Faker<AgeRating>().RuleFor(
            a => a.Censorship,
            f => f.Lorem.Word()
        );

        _movieGenerator = new Faker<Movie>()
            .RuleFor(m => m.Id, (f, m) => f.IndexFaker + 1)
            .RuleFor(m => m.Title, f => f.Lorem.Sentence())
            .RuleFor(m => m.ImageUrl, f => f.Image.PicsumUrl())
            .RuleFor(m => m.KinoUrl, f => f.Internet.Url())
            .RuleFor(m => m.Duration, f => f.Random.Int(60, 180))
            .RuleFor(m => m.PremiereDate, f => f.Date.Past().ToString())
            .RuleFor(m => m.AgeRating, f => _ageRatingGenerator.Generate());

        _hostGenerator = new Faker<Host>()
            .RuleFor(h => h.AuthId, f => f.Random.Uuid().ToString())
            .RuleFor(h => h.Email, f => f.Internet.Email())
            .RuleFor(h => h.Username, f => f.Internet.UserName());

        _showtimeGenerator = new Faker<Showtime>()
            .RuleFor(s => s.Id, (f, s) => f.IndexFaker + 1)
            .RuleFor(s => s.Movie, f => f.PickRandom(_movieGenerator.Generate(5)))
            .RuleFor(s => s.Cinema, f => f.PickRandom(_cinemaGenerator.Generate(5)))
            .RuleFor(s => s.Playtime, f => f.PickRandom(_playtimeGenerator.Generate(5)))
            .RuleFor(s => s.VersionTag, f => f.PickRandom(_versionTagGenerator.Generate(5)))
            .RuleFor(s => s.Room, f => f.PickRandom(_roomGenerator.Generate(5)));

        _participantGenerator = new Faker<Participant>()
            .RuleFor(p => p.AuthId, f => f.Random.Uuid().ToString())
            .RuleFor(p => p.Nickname, f => f.Internet.UserName())
            .RuleFor(p => p.Email, f => f.Internet.Email())
            .RuleFor(p => p.Note, f => f.Lorem.Sentence(3));

        _selectOptionGenerator = new Faker<SelectOption>()
            .RuleFor(o => o.VoteOption, f => f.Lorem.Word())
            .RuleFor(o => o.Color, f => f.Commerce.Color());

        JoinEventGenerator = new Faker<JoinEvent>().CustomInstantiator(f =>
        {
            var participants = _participantGenerator.Generate(f.Random.Int(0, 5));

            var joinEvent = new JoinEvent
            {
                HostId = f.Random.Uuid().ToString(),
                Title = f.Lorem.Sentence(3),
                Description = f.Lorem.Paragraph(3),
                Showtimes = _showtimeGenerator.Generate(f.Random.Int(5, 5)),
                Participants = participants,
                SelectOptions = _selectOptionGenerator.Generate(f.Random.Int(2, 5)),
                Deadline = f.Date.Future(),
                Host = _hostGenerator.Generate(),
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

            if (joinEvent.Showtimes.Where(j => j.Movie == null).Any())
            {
                throw new ArgumentException("There are no movies");
            }

            return joinEvent;
        });
    }
}
