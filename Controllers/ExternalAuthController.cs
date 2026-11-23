using System.Security.Claims;
using Agroskalka.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace Agroskalka.Controllers;

[Route("auth")]
public class ExternalAuthController : Controller
{
    private readonly SignInManager<User> _signInManager;
    private readonly UserManager<User> _userManager;

    public ExternalAuthController(SignInManager<User> signInManager, UserManager<User> userManager)
    {
        _signInManager = signInManager;
        _userManager = userManager;
    }

    [HttpGet("keycloak-login")]
    public IActionResult KeycloakLogin(string? returnUrl = "/")
    {
        var redirectUrl = Url.Action(nameof(KeycloakCallback), "ExternalAuth", new { returnUrl });
        var properties = _signInManager.ConfigureExternalAuthenticationProperties("Keycloak", redirectUrl);
        return Challenge(properties, "Keycloak");
    }

    [HttpGet("keycloak-callback")]
public async Task<IActionResult> KeycloakCallback(string? returnUrl = "/")
{
    var info = await _signInManager.GetExternalLoginInfoAsync();
    if (info == null)
    {
        return Redirect("/Identity/Account/Login");
    }

    var email = info.Principal.FindFirstValue(ClaimTypes.Email);
    if (string.IsNullOrEmpty(email))
    {
        await HttpContext.SignOutAsync(IdentityConstants.ExternalScheme);
        return BadRequest("Keycloak nie zwrócił adresu e-mail użytkownika.");
    }

    // 1. Najpierw szukamy po istniejącym powiązaniu OIDC (użytkownik już wcześniej logował się przez Keycloak)
    var user = await _userManager.FindByLoginAsync(info.LoginProvider, info.ProviderKey);

    if (user == null)
    {
        // 2. Brak powiązania OIDC — sprawdzamy, czy w bazie istnieje lokalne konto z tym e-mailem
        var existingUser = await _userManager.FindByEmailAsync(email);

        if (existingUser != null)
        {
            // Migracja istniejącego konta lokalnego — TYLKO jeśli Keycloak potwierdził e-mail
            var isEmailVerified = string.Equals(
                info.Principal.FindFirstValue("email_verified"),
                "true",
                StringComparison.OrdinalIgnoreCase);

            if (!isEmailVerified)
            {
                await HttpContext.SignOutAsync(IdentityConstants.ExternalScheme);
                TempData["ErrorMessage"] = "Potwierdź swój adres e-mail w Keycloak, aby połączyć go z istniejącym kontem.";
                return Redirect("/Identity/Account/Login");
            }

            // Podpinamy login z Keycloaka
            var addLoginResult = await _userManager.AddLoginAsync(existingUser, info);
            if (!addLoginResult.Succeeded)
            {
                await HttpContext.SignOutAsync(IdentityConstants.ExternalScheme);
                return BadRequest(string.Join(", ", addLoginResult.Errors.Select(e => e.Description)));
            }

            // Usuwamy lokalne hasło
            if (await _userManager.HasPasswordAsync(existingUser))
            {
                await _userManager.RemovePasswordAsync(existingUser);
            }

            // Wyłączamy lokalne 2FA
            if (await _userManager.GetTwoFactorEnabledAsync(existingUser))
            {
                await _userManager.SetTwoFactorEnabledAsync(existingUser, false);
            }

            // Fizycznie usuwamy rekordy z AspNetUserTokens (klucz aplikacji i kody zapasowe)
            const string internalLoginProvider = "[AspNetUserStore]";
            await _userManager.RemoveAuthenticationTokenAsync(existingUser, internalLoginProvider, "AuthenticatorKey");
            await _userManager.RemoveAuthenticationTokenAsync(existingUser, internalLoginProvider, "RecoveryCodes");

            await _signInManager.ForgetTwoFactorClientAsync();

            // Oznaczamy e-mail jako potwierdzony lokalnie
            if (!existingUser.EmailConfirmed)
            {
                existingUser.EmailConfirmed = true;
                await _userManager.UpdateAsync(existingUser);
            }

            user = existingUser;
        }
        else
        {
            // 3. Zupełnie nowy użytkownik — zakładamy rekord w AspNetUsers i podpinamy Keycloak
            user = new User
            {
                UserName = email,
                Email = email,
                EmailConfirmed = true,
                Name = info.Principal.FindFirstValue(ClaimTypes.GivenName) ?? "Użytkownik",
                Surname = info.Principal.FindFirstValue(ClaimTypes.Surname) ?? "Keycloak",
                IsActive = true,
                IsApproved = true
            };

            var createResult = await _userManager.CreateAsync(user);
            if (!createResult.Succeeded)
            {
                await HttpContext.SignOutAsync(IdentityConstants.ExternalScheme);
                return BadRequest(string.Join(", ", createResult.Errors.Select(e => e.Description)));
            }

            var addLoginResult = await _userManager.AddLoginAsync(user, info);
            if (!addLoginResult.Succeeded)
            {
                await HttpContext.SignOutAsync(IdentityConstants.ExternalScheme);
                return BadRequest(string.Join(", ", addLoginResult.Errors.Select(e => e.Description)));
            }
        }
    }

    // 4. Synchronizacja ról z Keycloaka
    var kcRoles = info.Principal.FindAll("roles").Select(c => c.Value)
        .Concat(info.Principal.FindAll(ClaimTypes.Role).Select(c => c.Value))
        .ToHashSet(StringComparer.OrdinalIgnoreCase);

    if (kcRoles.Contains("Admin"))
    {
        if (!await _userManager.IsInRoleAsync(user, "Admin"))
            await _userManager.AddToRoleAsync(user, "Admin");
    }
    else
    {
        if (!await _userManager.IsInRoleAsync(user, "Client"))
            await _userManager.AddToRoleAsync(user, "Client");
    }

    // 5. Logujemy użytkownika w Identity, zachowując id_token z Keycloaka do późniejszego wylogowania
    var authProps = new AuthenticationProperties { IsPersistent = false };
    var idToken = info.AuthenticationTokens?.FirstOrDefault(t => t.Name == "id_token")?.Value;
    if (!string.IsNullOrEmpty(idToken))
    {
        authProps.StoreTokens(new[]
        {
            new AuthenticationToken { Name = "id_token", Value = idToken }
        });
    }

    await _signInManager.SignInAsync(user, authProps);
    await HttpContext.SignOutAsync(IdentityConstants.ExternalScheme);

    return LocalRedirect(Url.IsLocalUrl(returnUrl) ? returnUrl : "/");
}
    
    [HttpPost("logout")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        var idToken = await HttpContext.GetTokenAsync("id_token");

        // Wylogowanie lokalne (Identity)
        await _signInManager.SignOutAsync();

        // Jeśli użytkownik był zalogowany przez Keycloaka, wyloguj go też tam
        if (!string.IsNullOrEmpty(idToken))
        {
            var props = new AuthenticationProperties { RedirectUri = "/" };
            props.StoreTokens(new[]
            {
                new AuthenticationToken { Name = "id_token", Value = idToken }
            });

            return SignOut(props, "Keycloak");
        }

        return Redirect("/");
    }
}