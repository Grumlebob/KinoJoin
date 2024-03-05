using System.Net;
using System.Net.Http.Json;
using Application.DTO;
using Bogus;
using FluentAssertions;

namespace Test.Monkey;

[Collection("MonkeyCollection")]
public class MonkeyTests : IAsyncLifetime
{
    private HttpClient _client;

    //Delegate used to call the ResetDatabaseAsync method from the factory, without having to expose the factory
    private readonly Func<Task> _resetDatabase;

    private readonly Faker<Domain.Entities.Monkey> _monkeyGenerator =
        new Faker<Domain.Entities.Monkey>()
            .RuleFor(m => m.Name, f => f.Name.FirstName())
            .RuleFor(m => m.Age, f => f.Random.Int(1, 20));

    public MonkeyTests(MonkeyServiceWebAppFactory factory)
    {
        _client = factory.HttpClient;
        _resetDatabase = factory.ResetDatabaseAsync;
    }

    [Fact]
    public async Task GetMonkey_ReturnsNotFound_WhenMonkeyDoesNotExist()
    {
        // Arrange: Prepare the request
        var nonExistingId = 999;
        // Act: Send the request
        var response = await _client.GetAsync($"/monkeys/{nonExistingId}");
        // Assert: Check the result
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    //Implement a test that creates 100 monkeys in the db, then deletes them all and checks if the GetAllAsync method returns an empty list
    [Fact]
    public async Task CreateGetDeleteMonkeyIntegrationFlow()
    {
        var monkeys = _monkeyGenerator.Generate(100);

        foreach (var monkey in monkeys)
        {
            var createResponse = await _client.PostAsJsonAsync(
                "/monkeys",
                new CreateMonkeyDto(monkey.Name, monkey.Age)
            );
            createResponse.EnsureSuccessStatusCode();
        }

        var GetMonkeysResponse = await _client.GetAsync("/monkeys");

        GetMonkeysResponse.EnsureSuccessStatusCode();

        var result = await GetMonkeysResponse.Content.ReadFromJsonAsync<IEnumerable<MonkeyDto>>();

        Assert.NotNull(result);
        result.Count().Should().Be(100);

        foreach (var monkey in result)
        {
            var deleteResponse = await _client.DeleteAsync($"/monkeys/{monkey.Id}");
            deleteResponse.EnsureSuccessStatusCode();
        }

        var GetMonkeysResponseAfterDelete = await _client.GetAsync("/monkeys");

        GetMonkeysResponseAfterDelete.EnsureSuccessStatusCode();

        var resultAfterDelete = await GetMonkeysResponseAfterDelete.Content.ReadFromJsonAsync<
            IEnumerable<MonkeyDto>
        >();

        Assert.NotNull(resultAfterDelete);
        resultAfterDelete.Count().Should().Be(0);
    }

    //We don't care about the InitializeAsync method, but needed to implement the IAsyncLifetime interface
    public Task InitializeAsync() => Task.CompletedTask;

    public Task DisposeAsync() => _resetDatabase();
}
