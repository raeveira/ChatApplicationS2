using System.Security.Claims;
using Application.Dtos;
using Application.Exceptions;
using Application.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;

namespace Frontend;

public static class AuthEndpoints
{
    public const string DisplayNameClaim = "display_name";

    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        // Form posts are used (instead of calling the service from a component) because only an HTTP response can set the auth cookie.
        app.MapPost("/auth/login", async (
            HttpContext context,
            AuthService authService,
            [FromForm] string? username,
            [FromForm] string? password) =>
        {
            try
            {
                var user = await authService.LoginAsync(
                    new LoginRequest(username ?? string.Empty, password ?? string.Empty),
                    context.RequestAborted);

                var identity = new ClaimsIdentity(
                [
                    new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                    new Claim(ClaimTypes.Name, user.Username),
                    new Claim(DisplayNameClaim, user.DisplayName ?? user.Username)
                ], CookieAuthenticationDefaults.AuthenticationScheme);

                await context.SignInAsync(
                    CookieAuthenticationDefaults.AuthenticationScheme,
                    new ClaimsPrincipal(identity));

                return Results.Redirect("/");
            }
            catch (InvalidCredentialsException)
            {
                return Results.Redirect("/login?error=invalid");
            }
            catch (Exception)
            {
                return Results.Redirect("/login?error=failed");
            }
        });

        app.MapPost("/auth/logout", async (HttpContext context) =>
        {
            await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return Results.Redirect("/login");
        });

        // The password is checked again so a stolen session can't delete the account.
        app.MapPost("/auth/delete-account", async (
            HttpContext context,
            UserService userService,
            [FromForm] string? password) =>
        {
            var userIdText = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!Guid.TryParse(userIdText, out var userId))
                return Results.Redirect("/login");

            try
            {
                await userService.DeleteAccountAsync(userId, password ?? string.Empty, context.RequestAborted);
            }
            catch (IncorrectPasswordException)
            {
                return Results.Redirect("/account?error=password");
            }
            catch (Exception)
            {
                return Results.Redirect("/account?error=failed");
            }

            await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return Results.Redirect("/login");
        }).RequireAuthorization();

        // Used when the cookie belongs to an account that no longer exists (for example after a database reset).
        app.MapGet("/auth/session-expired", async (HttpContext context) =>
        {
            await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return Results.Redirect("/login");
        });

        return app;
    }
}
