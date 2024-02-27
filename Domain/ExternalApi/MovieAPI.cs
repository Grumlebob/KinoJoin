using Newtonsoft.Json;

namespace Domain.ExternalApi;

public class MovieRoot
{
    [JsonProperty("content")] public MovieContentLevel Content { get; set; }
}

public class MovieContentLevel
{
    [JsonProperty("field_trailer")] public FieldTrailer field_trailer { get; set; }
}

public class FieldTrailer
{
    [JsonProperty("field_media_oembed_video")] public FieldMediaOembedVideo FieldMediaOembedVideo { get; set; }
}

public class FieldMediaOembedVideo
{
    [JsonProperty("src")] public string? trailerUrl { get; set; }
}