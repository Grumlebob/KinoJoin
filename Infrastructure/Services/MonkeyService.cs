using Application.DTO;
using Application.Interfaces;
using Domain.Entities;
using Infrastructure.Database;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Services;

public class MonkeyService : IMonkeyService
{
    private readonly DataContext _context;
    public MonkeyService(DataContext context)
    {
        _context = context;
    }
    public async Task<MonkeyDto?> GetAsync(int id)
    {
        var result = await _context.Monkeys.FirstOrDefaultAsync(m => m.Id == id);
        return result is null ? null : new MonkeyDto(result.Id, result.Name, result.Age);
    }

    public async Task CreateAsync(CreateMonkeyDto monkey)
    {
        await _context.Monkeys.AddAsync(new Monkey { Name = monkey.Name, Age = monkey.Age });
        await _context.SaveChangesAsync();
        Console.WriteLine("Created Done");
    }
}