using System.Net.Http.Json;
using Application.DTO;
using FluentAssertions;
using SimdLinq;

namespace Test.KinoJoin;

[Collection("KinoJoinCollection")]
public class KinoJoinTests : IAsyncLifetime
{
    private HttpClient _client;

    //Delegate used to call the ResetDatabaseAsync method from the factory, without having to expose the factory
    private readonly Func<Task> _resetDatabase;

    private readonly DataGenerator _dataGenerator = new();

    public KinoJoinTests(KinoJoinApiWebAppFactory factory)
    {
        _client = factory.HttpClient;
        _resetDatabase = factory.ResetDatabaseAsync;
    }

    [Fact]
    public async Task SimpleJoinEventTest()
    {
        var joinEvents = _dataGenerator.JoinEventGenerator.Generate(1000);
        foreach (var joinEvent in joinEvents)
        {
            var upsertDto = UpsertJoinEventDto.FromModelToUpsertDto(joinEvent);
            var createResponse = await _client.PutAsJsonAsync("api/events", upsertDto);
            createResponse.EnsureSuccessStatusCode();
        }
    }

    [Fact]
    public async Task SimdLinqTest()
    {
        var joinEvents = _dataGenerator.JoinEventGenerator.Generate(1000).Select(s => s.Id).ToList();

        var maxWithoutSimd = Enumerable.Max(joinEvents);
        var minWithoutSimd = Enumerable.Min(joinEvents);
        
        var (min, max) = SimdLinqExtensions.MinMax(joinEvents);
        var doesContain = SimdLinqExtensions.Contains(joinEvents,joinEvents[0]);
        
        maxWithoutSimd.Should().Be(max);
        minWithoutSimd.Should().Be(min);
        doesContain.Should().BeTrue();
    }

    //We don't care about the InitializeAsync method, but needed to implement the IAsyncLifetime interface
    public Task InitializeAsync() => Task.CompletedTask;

    public Task DisposeAsync() => _resetDatabase();
}
