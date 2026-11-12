// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#nullable disable

using System;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using Agroskalka.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
// Potwierdzanie przez e-mail (wyłączone):
// using System.Text;
// using System.Text.Encodings.Web;
// using Microsoft.AspNetCore.Identity.UI.Services;
// using Microsoft.AspNetCore.WebUtilities;

namespace Agroskalka.Areas.Identity.Pages.Account.Manage
{
    public class EmailModel : PageModel
    {
        private readonly UserManager<User> _userManager;
        private readonly SignInManager<User> _signInManager;
        // private readonly IEmailSender _emailSender;

        public EmailModel(
            UserManager<User> userManager,
            SignInManager<User> signInManager
            /*, IEmailSender emailSender */)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            // _emailSender = emailSender;
        }

        public string Email { get; set; }

        // public bool IsEmailConfirmed { get; set; }

        [TempData]
        public string StatusMessage { get; set; }

        [BindProperty]
        public InputModel Input { get; set; }

        public class InputModel
        {
            [Required(ErrorMessage = "Podaj nowy adres e-mail.")]
            [EmailAddress(ErrorMessage = "Nieprawidłowy adres e-mail.")]
            [Display(Name = "Nowy e-mail")]
            public string NewEmail { get; set; }
        }

        private async Task LoadAsync(User user)
        {
            var email = await _userManager.GetEmailAsync(user);
            Email = email;

            Input = new InputModel
            {
                NewEmail = email,
            };

            // IsEmailConfirmed = await _userManager.IsEmailConfirmedAsync(user);
        }

        public async Task<IActionResult> OnGetAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound($"Nie udało się załadować użytkownika o ID '{_userManager.GetUserId(User)}'.");
            }

            await LoadAsync(user);
            return Page();
        }

        public async Task<IActionResult> OnPostChangeEmailAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound($"Nie udało się załadować użytkownika o ID '{_userManager.GetUserId(User)}'.");
            }

            if (!ModelState.IsValid)
            {
                await LoadAsync(user);
                return Page();
            }

            var email = await _userManager.GetEmailAsync(user);
            if (Input.NewEmail != email)
            {
                // Zmiana od razu, bez potwierdzenia mailem
                var emailResult = await _userManager.SetEmailAsync(user, Input.NewEmail);
                if (!emailResult.Succeeded)
                {
                    foreach (var error in emailResult.Errors)
                    {
                        ModelState.AddModelError(string.Empty, error.Description);
                    }
                    await LoadAsync(user);
                    return Page();
                }

                // UserName = Email, więc zmieniamy oba
                var userNameResult = await _userManager.SetUserNameAsync(user, Input.NewEmail);
                if (!userNameResult.Succeeded)
                {
                    foreach (var error in userNameResult.Errors)
                    {
                        ModelState.AddModelError(string.Empty, error.Description);
                    }
                    await LoadAsync(user);
                    return Page();
                }

                await _signInManager.RefreshSignInAsync(user);
                StatusMessage = "Adres e-mail został zmieniony.";
                return RedirectToPage();

                /* Wersja z potwierdzeniem mailem (wyłączona):
                var userId = await _userManager.GetUserIdAsync(user);
                var code = await _userManager.GenerateChangeEmailTokenAsync(user, Input.NewEmail);
                code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(code));
                var callbackUrl = Url.Page(
                    "/Account/ConfirmEmailChange",
                    pageHandler: null,
                    values: new { area = "Identity", userId = userId, email = Input.NewEmail, code = code },
                    protocol: Request.Scheme);
                await _emailSender.SendEmailAsync(
                    Input.NewEmail,
                    "Potwierdź zmianę e-maila",
                    $"Potwierdź zmianę adresu e-mail, <a href='{HtmlEncoder.Default.Encode(callbackUrl)}'>klikając tutaj</a>.");

                StatusMessage = "Wysłano link potwierdzający zmianę e-maila. Sprawdź skrzynkę pocztową.";
                return RedirectToPage();
                */
            }

            StatusMessage = "Adres e-mail nie został zmieniony.";
            return RedirectToPage();
        }

        /* Wysyłanie e-maila weryfikacyjnego (wyłączone):
        public async Task<IActionResult> OnPostSendVerificationEmailAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound($"Nie udało się załadować użytkownika o ID '{_userManager.GetUserId(User)}'.");
            }

            if (!ModelState.IsValid)
            {
                await LoadAsync(user);
                return Page();
            }

            var userId = await _userManager.GetUserIdAsync(user);
            var email = await _userManager.GetEmailAsync(user);
            var code = await _userManager.GenerateEmailConfirmationTokenAsync(user);
            code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(code));
            var callbackUrl = Url.Page(
                "/Account/ConfirmEmail",
                pageHandler: null,
                values: new { area = "Identity", userId = userId, code = code },
                protocol: Request.Scheme);
            await _emailSender.SendEmailAsync(
                email,
                "Potwierdź e-mail",
                $"Potwierdź konto, <a href='{HtmlEncoder.Default.Encode(callbackUrl)}'>klikając tutaj</a>.");

            StatusMessage = "Wysłano wiadomość weryfikacyjną. Sprawdź skrzynkę pocztową.";
            return RedirectToPage();
        }
        */
    }
}