using System.Security.Claims;
using Agroskalka.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

public class AppClaimsPrincipalFactory : UserClaimsPrincipalFactory<User, Role>
{
    public AppClaimsPrincipalFactory(
        UserManager<User> userManager,
        RoleManager<Role> roleManager,
        IOptions<IdentityOptions> optionsAccessor)
        : base(userManager, roleManager, optionsAccessor)
    {
    }

    protected override async Task<ClaimsIdentity> GenerateClaimsAsync(User user)
    {
        var identity = await base.GenerateClaimsAsync(user);
        var logins = await UserManager.GetLoginsAsync(user);

        var isKeycloakUser = logins.Any(l =>
            l.LoginProvider.Equals("Keycloak", StringComparison.OrdinalIgnoreCase) ||
            l.LoginProvider.Equals("OpenIdConnect", StringComparison.OrdinalIgnoreCase));

        if (isKeycloakUser)
        {
            identity.AddClaim(new Claim("auth_provider", "Keycloak"));
        }

        return identity;
    }
}