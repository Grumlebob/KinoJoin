using Microsoft.AspNetCore.Components.Authorization;

namespace Presentation.Client.Services;

public static class UserInfoService
{
    /**
     * Get information about the currently logged in user info from an authentication state task,
     * which can be obtained as a cascading parameter in a component.
     * Will return null if the user is not logged in.
     */
    public static async Task<(string authId, string name, string email)?> GetUserInfoAsync(
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
