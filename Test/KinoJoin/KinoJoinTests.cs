using System.Net.Http.Json;
using Application.DTO;

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
            var UpsertDto = UpsertJoinEventDto.FromModelToUpsertDto(joinEvent);
            var createResponse = await _client.PutAsJsonAsync("/putJoinEvent", UpsertDto);
            createResponse.EnsureSuccessStatusCode();
        }
    }

    //We don't care about the InitializeAsync method, but needed to implement the IAsyncLifetime interface
    public Task InitializeAsync() => Task.CompletedTask;

    public Task DisposeAsync() => _resetDatabase();
}
