using Bogus;
using Domain.Entities;

namespace Test.KinoJoin;

public class DataGenerator
{
    private readonly Faker<Cinema> _cinemaGenerator;
    private readonly Faker<Movie> _movieGenerator;
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

        _showtimeGenerator = new Faker<Showtime>()
            .RuleFor(s => s.Id, (f, s) => f.IndexFaker + 1)
            .RuleFor(s => s.CinemaId, f => f.PickRandom(_cinemaGenerator.Generate()).Id)
            .RuleFor(s => s.PlaytimeId, f => f.PickRandom(_playtimeGenerator.Generate()).Id)
            .RuleFor(s => s.VersionTagId, f => f.PickRandom(_versionTagGenerator.Generate()).Id)
            .RuleFor(s => s.RoomId, f => f.PickRandom(_roomGenerator.Generate()).Id);

        _movieGenerator = new Faker<Movie>()
            .RuleFor(m => m.Id, (f, m) => f.IndexFaker + 1)
            .RuleFor(m => m.Title, f => f.Lorem.Sentence())
            .RuleFor(m => m.Showtimes, f => _showtimeGenerator.Generate(f.Random.Int(1, 3)));

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

        JoinEventGenerator = new Faker<JoinEvent>()
            .CustomInstantiator(f =>
            {
                var movies = _movieGenerator.Generate(f.Random.Int(1, 5));

                var joinEvent = new JoinEvent
                {
                    Id = f.Random.Int(1),
                    HostId = f.Random.Uuid().ToString(),
                    Title = f.Lorem.Sentence(),
                    Description = f.Lorem.Paragraph(),
                    Showtimes = movies.SelectMany(m => m.Showtimes!).ToList(),
                    Participants = _participantGenerator.Generate(f.Random.Int(1, 10)),
                    SelectOptions = _selectOptionGenerator.Generate(f.Random.Int(1, 3)),
                    Deadline = f.Date.Future(),
                    Host = null
                };

                if (joinEvent.Showtimes.Any())
                {
                    joinEvent.ChosenShowtimeId = f.PickRandom(joinEvent.Showtimes).Id;
                }
                
                // After creating JoinEvent, generate ParticipantVotes
                foreach (var participant in joinEvent.Participants)
                {
                    participant.VotedFor = new List<ParticipantVote>();
                    var numberOfVotes = f.Random.Int(1, joinEvent.Showtimes.Count);
                    for (int i = 0; i < numberOfVotes; i++)
                    {
                        participant.VotedFor.Add(new ParticipantVote
                        {
                            ParticipantId = participant.Id,
                            ShowtimeId = f.PickRandom(joinEvent.Showtimes).Id,
                            VoteIndex = f.Random.Int(0, joinEvent.Showtimes.Count-1)
                        });
                    }
                }

                // Set ChosenShowtimeId if Showtimes are available
                if (joinEvent.Showtimes.Any())
                {
                    joinEvent.ChosenShowtimeId = f.PickRandom(joinEvent.Showtimes).Id;
                }

                return joinEvent;
            });
    }
}