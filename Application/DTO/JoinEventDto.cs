namespace Application.DTO;

public class UpsertJoinEventDto
{
    public int? Id { get; set; } //If null, create new JoinEvent, else update existing
    public required string Title { get; set; }
    public string Description { get; set; } = string.Empty;
    public required UpsertHostDto Host { get; set; } //Ikke nullable, rigtige version skal ikke understøtte ukendt vært
    public List<UpsertShowtimeDto> Showtimes { get; set; } = [];
    public List<UpsertParticipantDto> Participants { get; set; } = [];
    public List<UpsertSelectOptionDto> SelectOptions { get; set; } = [];
    public int? ChosenShowtimeId { get; set; } //Nullable, on create it isn't set, on filling it is set.
    public DateTime Deadline { get; set; }

    public static UpsertJoinEventDto FromModelToUpsertDto(JoinEvent joinEvent)
    {
        var upsertJoinEventDto = new UpsertJoinEventDto
        {
            Id = joinEvent.Id, //1
            Title = joinEvent.Title, //+
            Description = joinEvent.Description,//+
            Host = new UpsertHostDto //+
            {
                AuthId = joinEvent.Host.AuthId, 
                Email = joinEvent.Host.AuthId,
                Username = joinEvent.Host.Username
            },
            ChosenShowtimeId = joinEvent.ChosenShowtimeId, //1
            Deadline = joinEvent.Deadline, //+
            Showtimes = joinEvent.Showtimes.Select(st => new UpsertShowtimeDto //+
            {
                Id = st.Id, //1
                Movie = new UpsertMovieDto
                {
                    Id = st.Movie.Id, //1
                    Title = st.Movie.Title,
                    AgeRating = st.Movie.AgeRating,
                    PremiereDate = st.Movie.PremiereDate,
                    KinoURL = st.Movie.KinoURL,
                    ImageUrl = st.Movie.ImageUrl,
                    Duration = st.Movie.Duration
                },
                Cinema = new UpsertCinemaDto
                {
                    Id   = st.Cinema.Id,
                    Name = st.Cinema.Name
                },
                Playtime = new UpsertPlaytimeDto
                {
                    StartTime = st.Playtime.StartTime
                },
                VersionTag = new UpsertVersionTagDto
                {
                    Type = st.VersionTag.Type
                },
                Room = new UpsertRoomDto
                {
                    Id = st.Room.Id, //1
                    Name = st.Room.Name
                }
            }).ToList(),
            Participants = joinEvent.Participants.Select(p => new UpsertParticipantDto //+
            {
                Id = p.Id, //1
                AuthId = p.AuthId,
                Email = p.Email,
                Nickname = p.Nickname,
                Note = p.Note,
                JoinEventId = joinEvent.Id, //1
                VotedFor = p.VotedFor.Select(v => new UpsertParticipantVoteDto
                {
                    ShowtimeId = v.ShowtimeId, //1
                    VoteIndex = v.VoteIndex //0
                }).ToList()
            }).ToList(),
            SelectOptions = joinEvent.SelectOptions.Select(s => new UpsertSelectOptionDto
            {
                Id = s.Id,
                VoteOption = s.VoteOption,
                Color = s.Color
            }).ToList()
        };
        
        return upsertJoinEventDto;
    }

    public static JoinEvent FromUpsertDtoToModel(UpsertJoinEventDto joinEventDto)
    {
        return new JoinEvent
        {
            Title = joinEventDto.Title,
            Description = joinEventDto.Description,
            Deadline = joinEventDto.Deadline,
            Host = new Host
            {
                AuthId = joinEventDto.Host.AuthId,
                Email = joinEventDto.Host.Email,
                Username = joinEventDto.Host.Username
            },
            HostId = joinEventDto.Host.AuthId,
            Participants = joinEventDto.Participants.Select(p => new Participant
            {
                AuthId = p.AuthId,
                Email = p.Email,
                Note = p.Note,
                Nickname = p.Nickname,
                JoinEventId = p.JoinEventId,
                VotedFor = p.VotedFor.Select(v => new ParticipantVote
                {
                    ShowtimeId = v.ShowtimeId,
                    VoteIndex = v.VoteIndex
                }).ToList()
            }).ToList(),
            SelectOptions = joinEventDto.SelectOptions.Select(s => new SelectOption
            {
                VoteOption = s.VoteOption,
                Color = s.Color
            }).ToList(),
            Showtimes = joinEventDto.Showtimes.Select(s => new Showtime
            {
                Id = s.Id,
                Playtime = new Playtime { StartTime = s.Playtime.StartTime },
                VersionTag = new VersionTag
                {
                    Type = s.VersionTag.Type
                },
                RoomId = s.Room.Id,
                Room = new Room
                {
                    Id = s.Room.Id,
                    Name = s.Room.Name
                },
                CinemaId = s.Cinema.Id,
                Cinema = new Cinema
                {
                    Id = s.Cinema.Id,
                    Name = s.Cinema.Name
                },
                MovieId = s.Movie.Id,
                Movie = new Movie
                {
                    Id = s.Movie.Id,
                    Title = s.Movie.Title,
                    AgeRating = s.Movie.AgeRating,
                    PremiereDate = s.Movie.PremiereDate,
                    KinoURL = s.Movie.KinoURL,
                    ImageUrl = s.Movie.ImageUrl,
                    Duration = s.Movie.Duration
                }
            }).ToList(),
            ChosenShowtimeId = joinEventDto.ChosenShowtimeId,
        };
    }
}

public class UpsertShowtimeDto
{
    public int Id { get; set; }
    public UpsertMovieDto Movie { get; set; }
    public UpsertRoomDto Room { get; set; }
    public UpsertCinemaDto Cinema { get; set; }
    public UpsertPlaytimeDto Playtime { get; set; }
    public UpsertVersionTagDto VersionTag { get; set; }
}

public class UpsertMovieDto
{
    public int Id { get; set; }
    public string Title { get; set; }
    public string? ImageUrl { get; set; }
    public string? KinoURL { get; set; }
    public int? Duration { get; set; }
    public string? PremiereDate { get; set; }
    public string? AgeRating { get; set; }
}

public class UpsertCinemaDto
{
    public int Id { get; set; }
    public string Name { get; set; }
}

public class UpsertRoomDto
{
    public int Id { get; set; }
    public string Name { get; set; }
}

public class UpsertParticipantDto
{
    public int? Id { get; set; } //If null, create new JoinEvent, else update existing
    public string? AuthId { get; set; }
    public int JoinEventId { get; set; }
    public string Nickname { get; set; }
    public string? Email { get; set; }
    public string? Note { get; set; }
    public List<UpsertParticipantVoteDto> VotedFor { get; set; } = [];
}

public class UpsertParticipantVoteDto
{
    public int ShowtimeId { get; set; }
    public int VoteIndex { get; set; }
}

public class UpsertSelectOptionDto
{
    public int? Id { get; set; } //If null, create new JoinEvent, else update existing
    public required string VoteOption { get; set; }
    public required string Color { get; set; }
}

public class UpsertHostDto
{
    public required string AuthId { get; set; }
    public string Username { get; set; }
    public string? Email { get; set; }
}

public class UpsertVersionTagDto
{
    public string Type { get; set; }
}

public class UpsertPlaytimeDto
{
    private DateTime _startTime;

    public DateTime StartTime
    {
        get => _startTime;
        set => _startTime = value.Kind == DateTimeKind.Utc ? value : value.ToUniversalTime();
    }
}