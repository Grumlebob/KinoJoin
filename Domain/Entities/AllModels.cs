using Microsoft.EntityFrameworkCore;

namespace Domain.Entities;

public class Host
{
    [Key, DatabaseGenerated(DatabaseGeneratedOption.None)]
    public string AuthId { get; set; }
    public string Username { get; set; }
    public string? Email { get; set; }
}

public class Participant
{
    [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }
    public string? AuthId { get; set; }
    //public int JoinEventId { get; set; }
    public string Nickname { get; set; }
    public string? Email { get; set; }
    public string? Note { get; set; }

    public ICollection<ParticipantVote> VotedFor { get; set; }
}

public class JoinEvent
{
    [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }
    public string HostId { get; set; }
    public string Title { get; set; }
    public string Description { get; set; }
    public List<Showtime> Showtimes { get; set; } = [];
    public int? ChosenShowtimeId { get; set; }
    public List<Participant> Participants { get; set; } = [];
    public List<SelectOption> SelectOptions { get; set; } = [];
    public int DefaultSelectOptionId { get; set; }

    private DateTime _deadline;

    public DateTime Deadline
    {
        get => _deadline;
        set => _deadline = value.Kind == DateTimeKind.Utc ? value : value.ToUniversalTime();
    }

    [ForeignKey("HostId")]
    public Host Host { get; set; }
    [ForeignKey("DefaultSelectOptionId")]
    public SelectOption DefaultSelectOption { get; set; }
}

public class Movie
{
    [Key, DatabaseGenerated(DatabaseGeneratedOption.None)]
    public int Id { get; set; }
    public string Title { get; set; }
    public string? ImageUrl { get; set; }
    public string? KinoURL { get; set; }
    public int? Duration { get; set; }
    public string? PremiereDate { get; set; }
    public string? AgeRating { get; set; }
}

public class Showtime
{
    [Key, DatabaseGenerated(DatabaseGeneratedOption.None)]
    public int Id { get; set; }

    public int MovieId { get; set; }
    public int CinemaId { get; set; }
    public int PlaytimeId { get; set; }
    public int VersionTagId { get; set; }
    public int RoomId { get; set; }

    //Foreign Keys
    [ForeignKey("VersionTagId")]
    public VersionTag VersionTag { get; set; }

    [ForeignKey("RoomId")]
    public Room Room { get; set; }

    [ForeignKey("MovieId")]
    public Movie Movie { get; set; }

    [ForeignKey("CinemaId")]
    public Cinema Cinema { get; set; }

    [ForeignKey("PlaytimeId")]
    public Playtime Playtime { get; set; }
}

public class ParticipantVote
{
    public int ParticipantId { get; set; }
    public int ShowtimeId { get; set; }
    public int SelectedOptionId { get; set; }
    [ForeignKey("SelectedOptionId")]
    public SelectOption SelectedOption { get; set; }
}

[Index(nameof(VoteOption), nameof(Color), IsUnique = true)]
public class SelectOption
{
    [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }
    public string VoteOption { get; set; }

    /// <summary>
    /// The color of the select option in hex format. In order to use a color in a tailwind class dynamically, the class must be used somewhere else in the project statically.
    /// For example, if the color is primary, and you want to use it as a background, the class bg-primary must be used somewhere in the project. Add the class to the dummy component "GenerateCustomTailwindColorsBeforeRunTime.razor" to use it, if it is not used anywhere else.
    /// </summary>
    public string Color { get; set; }
}

[Index(nameof(StartTime), IsUnique = true)]
public class Playtime
{
    [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    private DateTime _startTime;

    public DateTime StartTime
    {
        get => _startTime;
        set => _startTime = value.Kind == DateTimeKind.Utc ? value : value.ToUniversalTime();
    }
}

[Index(nameof(Type), IsUnique = true)]
public class VersionTag
{
    [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    public string Type { get; set; }
}

public class Cinema
{
    [Key, DatabaseGenerated(DatabaseGeneratedOption.None)]
    public int Id { get; set; }
    public string Name { get; set; }
}

public class Room
{
    [Key, DatabaseGenerated(DatabaseGeneratedOption.None)]
    public int Id { get; set; }

    public string Name { get; set; }
}

public class Genre
{
    [Key, DatabaseGenerated(DatabaseGeneratedOption.None)]
    public required int Id { get; set; }

    public string Name { get; set; }
}
