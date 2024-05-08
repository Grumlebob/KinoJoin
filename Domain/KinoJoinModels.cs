namespace Domain;

public class JoinEvent
{
    [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [MaxLength(260, ErrorMessage = "HostId kan højst være 260 tegn.")]
    public string HostId { get; set; } = string.Empty;

    [MaxLength(60, ErrorMessage = "Titel kan højst være 60 tegn.")]
    public string Title { get; set; } = string.Empty;

    [MaxLength(500, ErrorMessage = "Beskrivelse kan højst være 500 tegn.")]
    public string Description { get; set; } = string.Empty;
    public List<Showtime> Showtimes { get; set; } = [];
    public int? ChosenShowtimeId { get; set; }
    public List<Participant> Participants { get; set; } = [];
    public List<SelectOption> SelectOptions { get; set; } = [];
    public int DefaultSelectOptionId { get; set; }
    public DateTime Deadline { get; set; }

    [ForeignKey("HostId")]
    public Host Host { get; set; } = null!;

    [ForeignKey("DefaultSelectOptionId")]
    public SelectOption DefaultSelectOption { get; set; } = null!;
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

    [ForeignKey("VersionTagId")]
    public VersionTag VersionTag { get; set; } = null!;

    [ForeignKey("RoomId")]
    public Room Room { get; set; } = null!;

    [ForeignKey("MovieId")]
    public Movie Movie { get; set; } = null!;

    [ForeignKey("CinemaId")]
    public Cinema Cinema { get; set; } = null!;

    [ForeignKey("PlaytimeId")]
    public Playtime Playtime { get; set; } = null!;
}

public class Movie
{
    [Key, DatabaseGenerated(DatabaseGeneratedOption.None)]
    public int Id { get; set; }

    public int? AgeRatingId { get; set; }

    [MaxLength(500, ErrorMessage = "Title kan højst være 260 tegn.")]
    public required string Title { get; set; }

    [MaxLength(500, ErrorMessage = "Image url kan højst være 260 tegn.")]
    public string? ImageUrl { get; set; }

    [MaxLength(500, ErrorMessage = "Kino's info url kan højst være 260 tegn.")]
    public string? KinoUrl { get; set; } = string.Empty;
    public int DurationInMinutes { get; set; }

    [MaxLength(100, ErrorMessage = "PremiereDate kan højst være 260 tegn.")]
    public string? PremiereDate { get; set; }

    public bool IsSpecialShow { get; set; }

    [ForeignKey("AgeRatingId")]
    public AgeRating? AgeRating { get; set; }
}

[Index(nameof(Censorship), IsUnique = true)]
public class AgeRating
{
    [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [MaxLength(50, ErrorMessage = "Censorship kan højst være 260 tegn.")]
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

    [MaxLength(260, ErrorMessage = "Version Type kan højst være 260 tegn.")]
    public string Type { get; set; } = string.Empty;
}

public class Cinema
{
    [Key, DatabaseGenerated(DatabaseGeneratedOption.None)]
    public int Id { get; set; }

    [MaxLength(260, ErrorMessage = "Cinema Name kan højst være 260 tegn.")]
    public string Name { get; set; } = string.Empty;
}

public class Room
{
    [Key, DatabaseGenerated(DatabaseGeneratedOption.None)]
    public int Id { get; set; }

    [MaxLength(260, ErrorMessage = "Room name kan højst være 260 tegn.")]
    public string Name { get; set; } = string.Empty;
}

public class Genre
{
    [Key, DatabaseGenerated(DatabaseGeneratedOption.None)]
    public required int Id { get; set; }

    [MaxLength(260, ErrorMessage = "Genre name kan højst være 260 tegn.")]
    public string Name { get; set; } = string.Empty;
}

public class Host
{
    [Key, DatabaseGenerated(DatabaseGeneratedOption.None)]
    [MaxLength(260, ErrorMessage = "Host authentication ID kan højst være 260 tegn.")]
    public string AuthId { get; set; } = string.Empty;

    [MaxLength(260, ErrorMessage = "Username kan højst være 260 tegn.")]
    public string Username { get; set; } = string.Empty;

    [MaxLength(260, ErrorMessage = "Email kan højst være 260 tegn.")]
    public string? Email { get; set; }
}

public class Participant
{
    [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [MaxLength(260, ErrorMessage = "Participant authentication ID kan højst være 260 tegn.")]
    public string? AuthId { get; set; }
    public int JoinEventId { get; set; }

    [MaxLength(60, ErrorMessage = "Navn kan højst være 60 tegn.")]
    public string Nickname { get; set; } = string.Empty;

    [MaxLength(60, ErrorMessage = "Email kan højst være 60 tegn.")]
    public string? Email { get; set; }

    [MaxLength(500, ErrorMessage = "Note kan højst være 500 tegn.")]
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
    public SelectOption SelectedOption { get; set; } = null!;
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

    [MaxLength(260, ErrorMessage = "VoteOption kan højst være 260 tegn.")]
    public required string VoteOption { get; set; }

    /// <remarks>
    /// In order to use a color in a tailwind class dynamically, the class must be used somewhere else in the project statically.<br/>
    /// We do this by adding a comment above the line where it is generated like this: <br/>
    /// @* bg-green bg-red *@ <br/>
    /// &lt;comp class="bg-@variableWithColorString"&gt;&lt;/comp&gt;
    /// </remarks>
    [MaxLength(260, ErrorMessage = "Color kan højst være 260 tegn.")]
    public required string Color { get; set; }
}
