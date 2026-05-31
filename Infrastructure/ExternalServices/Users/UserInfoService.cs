namespace Infrastructure.ExternalServices.Users;

public class UserInfoService : IUserInfoService
{
    public async Task<(string authId, string name, string email)?> GetUserInfoAsync(
        Task<AuthenticationState> authenticationStateTask
    )
    {
        var authState = await authenticationStateTask;
        var user = authState.User;

        var isLoggedIn = user.Identity!.IsAuthenticated;
        if (!isLoggedIn)
        {
            return null;
        }
        var authId = user.Claims.FirstOrDefault(c => c.Type == "sub")?.Value!;
        var name = user.Claims.FirstOrDefault(c => c.Type == "nickname")?.Value!;
        var email = user.Claims.FirstOrDefault(c => c.Type == "email")?.Value!;

        return (authId, name, email);
    }
}
