using System.Security.Claims;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;

namespace StockSense.Client
{

    // This is a client-side AuthenticationStateProvider that determines the user's authentication state by
    // looking for data persisted in the page when it was rendered on the server. This authentication state will
    // be fixed for the lifetime of the WebAssembly application. So, if the user needs to log in or out, a full
    // page reload is required.
    //
    // This only provides a user name and email for display purposes. It does not actually include any tokens
    // that authenticate to the server when making subsequent requests. That works separately using a
    // cookie that will be included on HttpClient requests to the server.
    public class PersistentAuthenticationStateProvider : AuthenticationStateProvider
    {
        private static readonly Task<AuthenticationState> _unauthenticatedTask =
            Task.FromResult(new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity())));

        private Task<AuthenticationState> _authStateTask = _unauthenticatedTask;

        public PersistentAuthenticationStateProvider(PersistentComponentState state)
        {
            if (!state.TryTakeFromJson<UserInfo>(nameof(UserInfo), out var userInfo) || userInfo is null) return;

            var claims = new[] {
                new Claim(ClaimTypes.NameIdentifier, userInfo.UserId ?? ""),
                new Claim(ClaimTypes.Name, userInfo.Email ?? ""),
                new Claim(ClaimTypes.Email, userInfo.Email ?? ""),
                new Claim(ClaimTypes.Role, userInfo.Role ?? "")

            };

            _authStateTask = Task.FromResult(new AuthenticationState(new ClaimsPrincipal(
                new ClaimsIdentity(claims, authenticationType: "Cookie"))));
        }

        public override Task<AuthenticationState> GetAuthenticationStateAsync() => _authStateTask;

        public void NotifyLogout()
        {
            var anonymous = new ClaimsPrincipal(new ClaimsIdentity());
            var authState = Task.FromResult(new AuthenticationState(anonymous));
            NotifyAuthenticationStateChanged(authState);
        }
    }
}
