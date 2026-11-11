using Agroskalka.Data;
using Agroskalka.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Agroskalka.Pages.Admin.Tickets
{
    public class CheckModel : PageModel
    {
        private readonly AppDbContext _context;

        public CheckModel(AppDbContext context)
        {
            _context = context;
        }

        [BindProperty]
        public string Code { get; set; }

        // Właściwości przechowujące wynik wyszukiwania dla widoku
        public Ticket? ScannedTicket { get; set; }
        public string StatusMessage { get; set; }
        public bool IsError { get; set; }
        public bool IsAlreadyChecked { get; set; }

        public void OnGet(string code)
        {
            // Jeśli ktoś przekaże kod w linku url, 
            // zostanie on automatycznie wpisany w pole tekstowe.
            if (!string.IsNullOrEmpty(code))
            {
                Code = code;
            }
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (string.IsNullOrWhiteSpace(Code))
            {
                StatusMessage = "Wpisz kod biletu.";
                IsError = true;
                return Page();
            }

            ScannedTicket = await _context.Tickets
                .Include(t => t.User) 
                .FirstOrDefaultAsync(t => t.Code == Code);

            if (ScannedTicket == null)
            {
                StatusMessage = "BŁĄD: Nie znaleziono biletu o podanym kodzie!";
                IsError = true;
                return Page();
            }

            // 1. Tworzymy "koszyk" na błędy
            var errors = new List<string>();

            // 2. Wykonujemy wszystkie testy po kolei, nie przerywając działania
            if (ScannedTicket.IsChecked)
            {
                errors.Add("UWAGA! Ten bilet został już wykorzystany!");
                IsAlreadyChecked = true;
            }

            if (!ScannedTicket.IsPaid)
            {
                errors.Add("UWAGA! Ten bilet NIE ZOSTAŁ OPŁACONY!");
            }

            var dzisiaj = DateOnly.FromDateTime(DateTime.Today);
            if (ScannedTicket.Date != dzisiaj)
            {
                errors.Add($"BŁĄD: Bilet jest wystawiony na inny dzień ({ScannedTicket.Date:dd.MM.yyyy})!");
            }

            // 3. Jeśli zebraliśmy jakiekolwiek błędy, wyświetlamy je wszystkie na raz
            if (errors.Any())
            {
                IsError = true;
                // Łączymy wszystkie błędy używając tagu nowej linii <br/>
                StatusMessage = string.Join("<br/><br/>", errors); 
                return Page();
            }

            // 4. Jeśli lista błędów jest pusta, kasujemy bilet
            ScannedTicket.IsChecked = true;
            await _context.SaveChangesAsync();

            StatusMessage = "BILET WAŻNY! Pomyślnie zweryfikowano i wpuszczono klienta.";
            IsError = false;

            return Page();
        }
    }
}