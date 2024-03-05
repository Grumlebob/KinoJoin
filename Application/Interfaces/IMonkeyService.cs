namespace Application.Interfaces;

public interface IMonkeyService
{
    public Task<IEnumerable<MonkeyDto>> GetAllAsync();
    public Task<MonkeyDto?> GetAsync(int id);
    public Task<MonkeyDto> CreateAsync(CreateMonkeyDto monkey);
    public Task DeleteAsync(int id);
}
