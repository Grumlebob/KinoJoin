using System.Net;
using System.Net.Http.Json;
using Application.DTO;

namespace Test;

//https://youtu.be/zaRM0iIhJvs?t=1955
public class MonkeyTests : IClassFixture<MonkeyServiceWebAppFactory>
{
    private MonkeyServiceWebAppFactory _factory;
    private HttpClient _client;
    
    public MonkeyTests(MonkeyServiceWebAppFactory factory)
    {
        _factory = factory;
        _client = _factory.CreateClient();
    }

    [Fact]
    public async Task CreateMonkey()
    {
        var monkey = new CreateMonkeyDto("George", 5);
        
        var response = await _client.PostAsJsonAsync("/monkeys", monkey);
        
        response.EnsureSuccessStatusCode();
        
        //Get monkey id from response
        var createdMonkey = await response.Content.ReadFromJsonAsync<MonkeyDto>();
        
        Assert.NotNull(createdMonkey);
        Assert.Equal("George", createdMonkey!.Name);
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
    


}