using Application.DTO;
using Domain.Entities;

namespace Application.Interfaces;

public interface IMonkeyService
{
    public Task<Monkey?> Get(int id);
    public Task Create(CreateMonkeyDto monkey);
}