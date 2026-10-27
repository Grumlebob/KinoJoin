using System.Net;
using System.Text;

namespace Test.KinoJoin;

internal static class KinoApiFixture
{
    public const string ShowtimesJson = """
        {
          "content": {
            "content": {
              "content": [
                {
                  "id": 1,
                  "movies": [
                    {
                      "id": 1001,
                      "type": "movie",
                      "versions": [
                        {
                          "label": "2D",
                          "dates": [
                            {
                              "date": "27/10",
                              "showtimes": [
                                { "id": 5001, "available_seats": 8, "time": "18:00", "room": { "id": 1, "label": "Screen 1" } },
                                { "id": 5002, "available_seats": 9, "time": "20:00", "room": { "id": 1, "label": "Screen 1" } }
                              ]
                            }
                          ]
                        }
                      ],
                      "content": {
                        "field_censorship_icon": "12",
                        "label": "Fixture Movie One",
                        "field_playing_time": "100",
                        "field_poster": { "field_media_image": { "sources": [{ "srcset": "/movie-one.jpg" }] } },
                        "field_premiere": "2026-10-01",
                        "url": "/movie-one"
                      }
                    }
                  ]
                },
                {
                  "id": 2,
                  "movies": [
                    {
                      "id": 1001,
                      "type": "movie",
                      "versions": [
                        {
                          "label": "2D",
                          "dates": [
                            {
                              "date": "27/10",
                              "showtimes": [
                                { "id": 5003, "available_seats": 7, "time": "19:00", "room": { "id": 2, "label": "Screen 2" } }
                              ]
                            }
                          ]
                        }
                      ],
                      "content": {
                        "field_censorship_icon": "12",
                        "label": "Fixture Movie One",
                        "field_playing_time": "100",
                        "field_poster": { "field_media_image": { "sources": [{ "srcset": "/movie-one.jpg" }] } },
                        "field_premiere": "2026-10-01",
                        "url": "/movie-one"
                      }
                    }
                  ]
                },
                {
                  "id": 3,
                  "movies": [
                    {
                      "id": 1002,
                      "type": "movie",
                      "versions": [
                        {
                          "label": "2D",
                          "dates": [
                            {
                              "date": "27/10",
                              "showtimes": [
                                { "id": 5004, "available_seats": 6, "time": "17:00", "room": { "id": 3, "label": "Screen 3" } },
                                { "id": 5005, "available_seats": 5, "time": "21:00", "room": { "id": 3, "label": "Screen 3" } }
                              ]
                            }
                          ]
                        }
                      ],
                      "content": {
                        "field_censorship_icon": "15",
                        "label": "Fixture Movie Two",
                        "field_playing_time": "110",
                        "field_poster": { "field_media_image": { "sources": [{ "srcset": "/movie-two.jpg" }] } },
                        "field_premiere": "2026-10-02",
                        "url": "/movie-two"
                      }
                    }
                  ]
                }
              ]
            },
            "facets": {
              "cinemas": { "options": [
                { "key": 1, "value": "Fixture Cinema One" },
                { "key": 2, "value": "Fixture Cinema Two" },
                { "key": 3, "value": "Fixture Cinema Three" }
              ] },
              "movies": { "options": [
                { "key": 1001, "value": "Fixture Movie One" },
                { "key": 1002, "value": "Fixture Movie Two" }
              ] },
              "versions": { "options": [{ "value": "2D" }] },
              "genres": { "options": [
                { "key": 96, "value": "Action" },
                { "key": 35, "value": "Comedy" }
              ] }
            }
          }
        }
        """;
}

internal sealed class KinoApiFixtureMessageHandler : HttpMessageHandler
{
    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken
    )
    {
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            RequestMessage = request,
            Content = new StringContent(
                KinoApiFixture.ShowtimesJson,
                Encoding.UTF8,
                "application/json"
            ),
        };

        return Task.FromResult(response);
    }
}
