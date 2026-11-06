using Agroskalka.Data;
using Agroskalka.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Agroskalka.Pages.Admin.Tariffs
{
    public class CreateModel : PageModel
    {
        private readonly AppDbContext _context;

        public CreateModel(AppDbContext context)
        {
            _context = context;
        }

        [BindProperty]
        public Tariff Tariff { get; set; } = default!;

        public IActionResult OnGet()
        {
            Tariff = new Tariff { ValidFrom = DateTime.UtcNow.Date };
            return Page();
        }

       public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            // Wymuszenie strefy UTC dla daty przychodzącej z formularza
            Tariff.ValidFrom = DateTime.SpecifyKind(Tariff.ValidFrom, DateTimeKind.Utc);

            // 1. Opcjonalnie: Zablokuj dodanie dwóch cenników tego samego dnia
            var existingExact = await _context.Tariffs.FirstOrDefaultAsync(t => t.ValidFrom == Tariff.ValidFrom);
            if (existingExact != null)
            {
                ModelState.AddModelError("Tariff.ValidFrom", "Istnieje już zaplanowany cennik na ten konkretny dzień.");
                return Page();
            }

            // 2. Znajdź cennik, który obowiązuje BEZPOŚREDNIO PRZED nowym
            var previousTariff = await _context.Tariffs
                .Where(t => t.ValidFrom < Tariff.ValidFrom)
                .OrderByDescending(t => t.ValidFrom)
                .FirstOrDefaultAsync();

            if (previousTariff != null)
            {
                // Skracamy czas życia poprzedniego cennika
                previousTariff.ValidTo = DateTime.SpecifyKind(Tariff.ValidFrom.AddDays(-1), DateTimeKind.Utc);
            }

            // 3. Znajdź cennik, który zaczyna się BEZPOŚREDNIO PO nowym
            var nextTariff = await _context.Tariffs
                .Where(t => t.ValidFrom > Tariff.ValidFrom)
                .OrderBy(t => t.ValidFrom)
                .FirstOrDefaultAsync();

            if (nextTariff != null)
            {
                // Nowy cennik kończy się dzień przed startem następnego
                Tariff.ValidTo = DateTime.SpecifyKind(nextTariff.ValidFrom.AddDays(-1), DateTimeKind.Utc);
            }
            else
            {
                // Jeśli nie ma nowszego cennika w przyszłości, nowy jest bezterminowy
                Tariff.ValidTo = DateTime.SpecifyKind(DateTime.MaxValue, DateTimeKind.Utc);
            }

            Tariff.IsActive = true;

            _context.Tariffs.Add(Tariff);
            await _context.SaveChangesAsync();

            return RedirectToPage("./Index");
        }
    }
}