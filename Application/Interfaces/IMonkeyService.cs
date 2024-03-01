namespace Application.Interfaces;

public interface IMonkeyService
{
    public Task<MonkeyDto?> GetAsync(int id);
    public Task CreateAsync(CreateMonkeyDto monkey);
}
