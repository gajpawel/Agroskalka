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
            return BadRequest("Keycloak nie zwrócił adresu e-mail użytkownika.");
        }

        // 1. Szukamy użytkownika po powiązaniu OIDC lub po adresie e-mail
        var user = await _userManager.FindByLoginAsync(info.LoginProvider, info.ProviderKey)
                   ?? await _userManager.FindByEmailAsync(email);

        // 2. Jeśli to pierwsze logowanie, zakładamy rekord w AspNetUsers
        if (user == null)
        {
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
                return BadRequest(string.Join(", ", createResult.Errors.Select(e => e.Description)));
            }
        }

        // 3. Zapisujemy powiązanie w AspNetUserLogins, jeśli jeszcze go nie ma
        var existingLogins = await _userManager.GetLoginsAsync(user);
        if (!existingLogins.Any(l => l.LoginProvider == info.LoginProvider && l.ProviderKey == info.ProviderKey))
        {
            await _userManager.AddLoginAsync(user, info);
        }

        // 4. Synchronizacja ról z Keycloaka (z claimu "roles" skonfigurowanego wcześniej)
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

        // 5. Logujemy użytkownika standardowym ciasteczkiem Identity i czyścimy tymczasowe ciasteczko OIDC
        await _signInManager.SignInAsync(user, isPersistent: false);
        await HttpContext.SignOutAsync(IdentityConstants.ExternalScheme);

        return LocalRedirect(Url.IsLocalUrl(returnUrl) ? returnUrl : "/");
    }
}