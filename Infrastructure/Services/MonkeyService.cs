﻿namespace Infrastructure.Services;

public class MonkeyService : IMonkeyService
{
    private readonly MonkeyContext _context;

    public MonkeyService(MonkeyContext context)
    {
        _context = context;
    }

    public async Task<MonkeyDto?> GetAsync(int id)
    {
        var result = await _context.Monkeys.FirstOrDefaultAsync(m => m.Id == id);
        return result is null ? null : new MonkeyDto(result.Id, result.Name, result.Age);
    }

    public async Task<MonkeyDto> CreateAsync(CreateMonkeyDto monkey)
    {
        await _context.Monkeys.AddAsync(new Monkey { Name = monkey.Name, Age = monkey.Age });
        await _context.SaveChangesAsync();
        var id = await _context.Monkeys.MaxAsync(m => m.Id);
        return new MonkeyDto(id, monkey.Name, monkey.Age);
    }
}
