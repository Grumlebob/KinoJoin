using Microsoft.AspNetCore.Components.Authorization;

namespace Application.Interfaces;

/// <summary>
/// This service handles the information used by Auth0 to identify the user.
/// </summary>
public interface IUserInfoService
{
    /// <summary>
    /// Get information about the currently logged in user info from an authentication state task,
    /// which can be obtained as a cascading parameter in a component.
    /// Will return null if the user is not logged in.
    /// </summary>
    Task<(string authId, string name, string email)?> GetUserInfoAsync(
        Task<AuthenticationState> authenticationStateTask
    );
}
