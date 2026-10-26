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

        return app;
    }
}
