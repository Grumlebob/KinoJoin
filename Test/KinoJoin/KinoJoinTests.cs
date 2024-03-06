using System.Net;
using System.Net.Http.Json;
using Application.DTO;
using Bogus;
using Domain.Entities;
using FluentAssertions;
using Test.Monkey;

namespace Test.KinoJoin;

[Collection("KinoJoinCollection")]
public class KinoJoinTests : IAsyncLifetime
{
    private HttpClient _client;

    //Delegate used to call the ResetDatabaseAsync method from the factory, without having to expose the factory
    private readonly Func<Task> _resetDatabase;

    private readonly DataGenerator _dataGenerator = new();

    public KinoJoinTests(MonkeyServiceWebAppFactory factory)
    {
        _client = factory.HttpClient;
        _resetDatabase = factory.ResetDatabaseAsync;
    }

    [Fact]
    public async Task SimpleJoinEventTest()
    {
        var JoinEvents = _dataGenerator.JoinEventGenerator.Generate(10);
        foreach (var JoinEvent in JoinEvents)
        {
            var UpsertDto = UpsertJoinEventDto.FromModelToUpsertDto(JoinEvent);
            var createResponse = await _client.PutAsJsonAsync("/putJoinEvent", UpsertDto);
            createResponse.EnsureSuccessStatusCode();
            var id = await createResponse.Content.ReadFromJsonAsync<int>();
            var responseContent = await createResponse.Content.ReadAsStringAsync();
        }
        Console.WriteLine("e");
        var a = 1;
        var v = "efdef";


    }

    //We don't care about the InitializeAsync method, but needed to implement the IAsyncLifetime interface
    public Task InitializeAsync() => Task.CompletedTask;

    public Task DisposeAsync() => _resetDatabase();
}
