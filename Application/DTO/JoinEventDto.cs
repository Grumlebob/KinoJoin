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
        return new UpsertJoinEventDto
        {
            Id = joinEvent.Id,
            Title = joinEvent.Title,
            Description = joinEvent.Description,
            Host = new UpsertHostDto
            {
                AuthId = joinEvent.Host.AuthId,
                Email = joinEvent.Host.AuthId,
                Username = joinEvent.Host.Username
            },
            ChosenShowtimeId = joinEvent.ChosenShowtimeId,
            Deadline = joinEvent.Deadline,
            Showtimes = joinEvent.Showtimes.Select(st => new UpsertShowtimeDto
            {
                Id = st.Id,
                MovieId = st.MovieId,
                CinemaId = st.CinemaId,
                Playtime = new UpsertPlaytimeDto
                {
                    StartTime = st.Playtime.StartTime
                },
                VersionTagId = st.VersionTagId,
                RoomId = st.RoomId
            }).ToList(),
            Participants = joinEvent.Participants.Select(p => new UpsertParticipantDto
            {
                Id = p.Id,
                AuthId = p.AuthId,
                Email = p.Email,
                Nickname = p.Nickname,
                Note = p.Note,
                JoinEventId = joinEvent.Id,
                VotedFor = p.VotedFor.Select(v => new UpsertParticipantVoteDto
                {
                    ShowtimeId = v.ShowtimeId,
                    VoteIndex = v.VoteIndex
                }).ToList()
            }).ToList(),
            SelectOptions = joinEvent.SelectOptions.Select(s => new UpsertSelectOptionDto
            {
                Id = s.Id,
                VoteOption = s.VoteOption,
                Color = s.Color
            }).ToList()
        };
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
                VersionTagId = s.VersionTagId,
                RoomId = s.RoomId,
                CinemaId = s.CinemaId,
                MovieId = s.MovieId
            }).ToList(),
            ChosenShowtimeId = joinEventDto.ChosenShowtimeId,
        };
    }
}

public class UpsertShowtimeDto
{
    public int Id { get; set; }
    public int MovieId { get; set; }
    public int CinemaId { get; set; }
    public UpsertPlaytimeDto Playtime { get; set; }
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

public class UpsertPlaytimeDto
{
    private DateTime _startTime;

    public DateTime StartTime
    {
        get => _startTime;
        set => _startTime = value.Kind == DateTimeKind.Utc ? value : value.ToUniversalTime();
    }
}