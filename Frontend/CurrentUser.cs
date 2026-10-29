using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;

namespace Frontend;

// Gives components the id of the logged-in user. The id always comes from the (tamper-proof) auth cookie.
public class CurrentUser(AuthenticationStateProvider authenticationStateProvider)
{
    public async Task<Guid> GetIdAsync()
    {
        AuthenticationState state = await authenticationStateProvider.GetAuthenticationStateAsync();
        string? idText = state.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        return Guid.TryParse(idText, out Guid userId)
            ? userId
            : throw new InvalidOperationException("The user is not logged in.");
    }
}
