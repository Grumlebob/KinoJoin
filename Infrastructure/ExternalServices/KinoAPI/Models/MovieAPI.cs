namespace Infrastructure.ExternalServices.KinoAPI.Models;

/// <summary>
/// This is when we are calling the api for a single specific movie, to include more details, in our case trailer link.
/// </summary>
public class MovieApiRoot
{
    [JsonProperty("content")]
    public required MovieApiContentLevel ApiContent;
}

public class MovieApiContentLevel
{
    [JsonProperty("field_trailer")]
    public required MovieApiFieldTrailer MovieApiFieldTrailer;
}

public class MovieApiFieldTrailer
{
    [JsonProperty("field_media_oembed_video")]
    public required MovieApiFieldMediaOembedVideo MovieApiFieldMediaOembedVideo;
}

public class MovieApiFieldMediaOembedVideo
{
    [JsonProperty("src")]
    public string? TrailerUrl;
}
