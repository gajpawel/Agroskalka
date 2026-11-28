
using System;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using Agroskalka.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Logging;

namespace Agroskalka.Areas.Identity.Pages.Account
{
    [EnableRateLimiting("mfa")]
    public class LoginWith2faModel : PageModel
    {
        private readonly SignInManager<User> _signInManager;
        private readonly UserManager<User> _userManager;
        private readonly ILogger<LoginWith2faModel> _logger;

        public LoginWith2faModel(
            SignInManager<User> signInManager,
            UserManager<User> userManager,
            ILogger<LoginWith2faModel> logger)
        {
            _signInManager = signInManager;
            _userManager = userManager;
            _logger = logger;
        }

        [BindProperty]
        public InputModel Input { get; set; }

        public bool RememberMe { get; set; }

        public string ReturnUrl { get; set; }

        public class InputModel
        {
            [Required(ErrorMessage = "Wpisz kod weryfikacyjny.")]
            [StringLength(
                7,
                ErrorMessage = "Kod weryfikacyjny musi mieć od {2} do {1} znaków.",
                MinimumLength = 6
            )]
            [DataType(DataType.Text)]
            [Display(Name = "Kod z aplikacji Authenticator")]
            public string TwoFactorCode { get; set; }

            [Display(Name = "Zapamiętaj to urządzenie")]
            public bool RememberMachine { get; set; }
        }

        public async Task<IActionResult> OnGetAsync(
            bool rememberMe,
            string returnUrl = null)
        {
            // Sprawdzenie, czy użytkownik przeszedł pierwszy etap logowania.
            var user = await _signInManager
                .GetTwoFactorAuthenticationUserAsync();

            if (user == null)
            {
                throw new InvalidOperationException(
                    "Nie udało się odnaleźć użytkownika do weryfikacji dwuetapowej."
                );
            }

            ReturnUrl = returnUrl;
            RememberMe = rememberMe;

            return Page();
        }

        public async Task<IActionResult> OnPostAsync(
            bool rememberMe,
            string returnUrl = null)
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            returnUrl ??= Url.Content("~/");

            // Pobranie użytkownika oczekującego na drugi etap logowania.
            var user = await _signInManager
                .GetTwoFactorAuthenticationUserAsync();

            if (user == null)
            {
                throw new InvalidOperationException(
                    "Nie udało się odnaleźć użytkownika do weryfikacji dwuetapowej."
                );
            }

            // Usunięcie spacji i myślników z wpisanego kodu.
            var authenticatorCode = Input.TwoFactorCode
                .Replace(" ", string.Empty)
                .Replace("-", string.Empty);

            // Weryfikacja kodu jednorazowego z aplikacji Authenticator.
            var result = await _signInManager.TwoFactorAuthenticatorSignInAsync(
                authenticatorCode,
                rememberMe,
                Input.RememberMachine
            );

            if (result.Succeeded)
            {
                _logger.LogInformation(
                    "Użytkownik o ID '{UserId}' zalogował się pomyślnie przy użyciu MFA.",
                    user.Id
                );

                return LocalRedirect(returnUrl);
            }

            if (result.IsLockedOut)
            {
                _logger.LogWarning(
                    "Konto użytkownika o ID '{UserId}' zostało zablokowane.",
                    user.Id
                );

                return RedirectToPage("./Lockout");
            }

            _logger.LogWarning(
                "Użytkownik o ID '{UserId}' podał nieprawidłowy kod MFA.",
                user.Id
            );

            ModelState.AddModelError(
                string.Empty,
                "Nieprawidłowy kod weryfikacyjny. Sprawdź kod w aplikacji Authenticator i spróbuj ponownie."
            );

            return Page();
        }
    }
}
