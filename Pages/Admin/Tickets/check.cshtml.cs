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

        public async Task<IActionResult> OnPostAsync()
        {
            if (string.IsNullOrWhiteSpace(Code))
            {
                StatusMessage = "Wpisz kod biletu.";
                IsError = true;
                return Page();
            }

            // Szukamy biletu w bazie, od razu "dołączając" użytkownika, aby znać właściciela.
            ScannedTicket = await _context.Tickets
                .Include(t => t.User) 
                .FirstOrDefaultAsync(t => t.Code == Code);

            if (ScannedTicket == null)
            {
                StatusMessage = "BŁĄD: Nie znaleziono biletu o podanym kodzie!";
                IsError = true;
                return Page();
            }

            // Sprawdzamy, czy bilet nie został już skasowany
            if (ScannedTicket.IsChecked)
            {
                StatusMessage = "UWAGA! Ten bilet został już wykorzystany!";
                IsError = true;
                IsAlreadyChecked = true;
                return Page();
            }

            //  Ostrzeżenie, jeśli bilet jest nieopłacony
            if (!ScannedTicket.IsPaid)
            {
                StatusMessage = "UWAGA! Ten bilet istnieje, ale NIE ZOSTAŁ OPŁACONY!";
                IsError = true;
                return Page();
            }

            // Jeśli wszystkie testy minęły pomyślnie, "kasujemy" bilet
            ScannedTicket.IsChecked = true;
            await _context.SaveChangesAsync();

            StatusMessage = "BILET WAŻNY! Pomyślnie zweryfikowano i wpuszczono klienta.";
            IsError = false;

            return Page();
        }
    }
}