using Application.DTO;
using Application.Interfaces;
using Domain.Entities;
using Infrastructure.Database;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Services;

public class MonkeyService(DataContext context) : IMonkeyService
{
    public async Task<MonkeyDto?> Get(int id)
    {
        var result = await context.Monkeys.FirstOrDefaultAsync(m => m.Id == id);
        return result is null ? null : new MonkeyDto(result.Id, result.Name, result.Age);
    }

    public async Task Create(CreateMonkeyDto monkey)
    {
        await context.Monkeys.AddAsync(new Monkey { Id = 69, Name = monkey.Name, Age = monkey.Age });
    }
}