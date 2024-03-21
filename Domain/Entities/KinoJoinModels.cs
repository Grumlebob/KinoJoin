using Microsoft.EntityFrameworkCore;

namespace Domain.Entities;

public class JoinEvent
{
    [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }
    public string HostId { get; set; } = string.Empty;

    [MaxLength(60, ErrorMessage = "Titel kan høst være 60 tegn.")]
    public string Title { get; set; } = string.Empty;

    [MaxLength(500, ErrorMessage = "Beskrivelse kan høst være 500 tegn.")]
    public string Description { get; set; } = string.Empty;
    public List<Showtime> Showtimes { get; set; } = [];
    public int? ChosenShowtimeId { get; set; }
    public List<Participant>? Participants { get; set; } = [];
    public List<SelectOption> SelectOptions { get; set; } = [];
    public int DefaultSelectOptionId { get; set; }

    public DateTime Deadline { get; set; }

    [ForeignKey("HostId")]
    public Host? Host { get; set; }

    [ForeignKey("DefaultSelectOptionId")]
    public SelectOption? DefaultSelectOption { get; set; }
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
    public VersionTag? VersionTag { get; set; }

    [ForeignKey("RoomId")]
    public Room? Room { get; set; }

    [ForeignKey("MovieId")]
    public Movie? Movie { get; set; }

    [ForeignKey("CinemaId")]
    public Cinema? Cinema { get; set; }

    [ForeignKey("PlaytimeId")]
    public Playtime? Playtime { get; set; }
}

public class Movie
{
    [Key, DatabaseGenerated(DatabaseGeneratedOption.None)]
    public int Id { get; set; }

    public required string Title { get; set; }
    public string? ImageUrl { get; set; }
    public string? KinoUrl { get; set; } = string.Empty;
    public int? Duration { get; set; }
    public string? PremiereDate { get; set; }
    public AgeRating? AgeRating { get; set; }
}

[Index(nameof(Censorship), IsUnique = true)]
public class AgeRating
{
    [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    public string Censorship { get; set; } = string.Empty;
}

[Index(nameof(StartTime), IsUnique = true)]
public class Playtime
{
    [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    public DateTime StartTime { get; set; }
}

[Index(nameof(Type), IsUnique = true)]
public class VersionTag
{
    [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    public string Type { get; set; } = string.Empty;
}

public class Cinema
{
    [Key, DatabaseGenerated(DatabaseGeneratedOption.None)]
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;
}

public class Room
{
    [Key, DatabaseGenerated(DatabaseGeneratedOption.None)]
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;
}

public class Genre
{
    [Key, DatabaseGenerated(DatabaseGeneratedOption.None)]
    public required int Id { get; set; }

    public string Name { get; set; } = string.Empty;
}

public class Host
{
    [Key, DatabaseGenerated(DatabaseGeneratedOption.None)]
    public string AuthId { get; set; } = string.Empty;

    public string Username { get; set; } = string.Empty;
    public string? Email { get; set; }
}

public class Participant
{
    [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    public string? AuthId { get; set; }
    public int JoinEventId { get; set; }

    [MaxLength(60, ErrorMessage = "Navn kan højst være 60 tegn.")]
    public string Nickname { get; set; } = string.Empty;

    [MaxLength(60, ErrorMessage = "Email kan høst være 60 tegn.")]
    public string? Email { get; set; }

    [MaxLength(500, ErrorMessage = "Note kan høst være 500 tegn.")]
    public string? Note { get; set; }
    public ICollection<ParticipantVote> VotedFor { get; set; } = [];
}

/// <summary>
/// Represents what option a user has selected for a showtime, when voting.
/// </summary>
public class ParticipantVote
{
    public int ParticipantId { get; set; }
    public int ShowtimeId { get; set; }
    public int SelectedOptionId { get; set; }

    [ForeignKey("SelectedOptionId")]
    public SelectOption? SelectedOption { get; set; }
}

/// <summary>
/// This model reflects the state of the individual showtime checkboxes on the UI, what is chosen when it is clicked.
/// <example>
/// VoteOption: "I can participate in this event"
/// <br/>
/// Color: primary
/// </example>
/// </summary>
[Index(nameof(VoteOption), nameof(Color), IsUnique = true)]
public class SelectOption
{
    [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    public required string VoteOption { get; set; }

    /// <remark>
    /// In order to use a color in a tailwind class dynamically, the class must be used somewhere else in the project statically.
    /// Add new colors to GenerateCustomTailwindColorsBeforeRunTime.razor to use them
    /// </remark>
    public required string Color { get; set; }
}
