using Application.DTO;

namespace Application.Interfaces;

public interface IMonkeyService
{
    public Task<MonkeyDto?> Get(int id);
    public Task Create(CreateMonkeyDto monkey);
}