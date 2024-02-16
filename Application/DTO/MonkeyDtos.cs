namespace Application.DTO;

public record MonkeyDto(int Id, string Name, int Age);

public record CreateMonkeyDto(string Name, int Age);
