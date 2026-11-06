using Agroskalka.Data;
using Agroskalka.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Agroskalka.Pages.Admin.Schedules
{
    public class CreateModel : PageModel
    {
        private readonly AppDbContext _context;

        public CreateModel(AppDbContext context)
        {
            _context = context;
        }

        [BindProperty]
        public DateTime ValidFrom { get; set; }

        // Słownik wiążący dni tygodnia z ustawieniami z formularza
        [BindProperty]
        public Dictionary<DayOfWeek, DayConfig> Days { get; set; } = new();

        public class DayConfig
        {
            public bool IsOpen { get; set; }
            public TimeOnly OpenTime { get; set; } = new TimeOnly(9, 0);
            public TimeOnly CloseTime { get; set; } = new TimeOnly(17, 0);
        }

        public IActionResult OnGet()
        {
            ValidFrom = DateTime.UtcNow.Date;
            
            // Inicjalizacja formularza dla każdego dnia
            var daysOfWeek = new[] { DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday, DayOfWeek.Saturday, DayOfWeek.Sunday };
            foreach (var day in daysOfWeek)
            {
                Days[day] = new DayConfig { IsOpen = true };
            }

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var utcValidFrom = DateTime.SpecifyKind(ValidFrom, DateTimeKind.Utc);

            // Zabezpieczenie przed dublowaniem daty
            if (await _context.Schedules.AnyAsync(s => s.ValidFrom == utcValidFrom))
            {
                ModelState.AddModelError("ValidFrom", "Harmonogram na ten dzień już istnieje.");
                return Page();
            }

            // --- 1. Aktualizacja Osi Czasu (dla wszystkich rekordów z danej "paczki" sezonowej) ---
            var previousSchedules = await _context.Schedules
                .Where(s => s.ValidFrom < utcValidFrom)
                .ToListAsync();

            if (previousSchedules.Any())
            {
                var lastValidFrom = previousSchedules.Max(s => s.ValidFrom);
                var schedulesToClose = previousSchedules.Where(s => s.ValidFrom == lastValidFrom);
                foreach (var s in schedulesToClose)
                {
                    s.ValidTo = DateTime.SpecifyKind(utcValidFrom.AddDays(-1), DateTimeKind.Utc);
                }
            }

            var nextSchedules = await _context.Schedules
                .Where(s => s.ValidFrom > utcValidFrom)
                .OrderBy(s => s.ValidFrom)
                .ToListAsync();

            var newValidTo = nextSchedules.Any() 
                ? DateTime.SpecifyKind(nextSchedules.First().ValidFrom.AddDays(-1), DateTimeKind.Utc) 
                : DateTime.SpecifyKind(DateTime.MaxValue, DateTimeKind.Utc);

            // --- 2. Grupowanie dni o tych samych godzinach ---
            var openDays = Days.Where(d => d.Value.IsOpen).ToList();
            var groupedSchedules = openDays.GroupBy(d => new { d.Value.OpenTime, d.Value.CloseTime });

            foreach (var group in groupedSchedules)
            {
                var schedule = new Schedule
                {
                    ValidFrom = utcValidFrom,
                    ValidTo = newValidTo,
                    OpenTime = group.Key.OpenTime,
                    CloseTime = group.Key.CloseTime,
                    Monday = group.Any(d => d.Key == DayOfWeek.Monday),
                    Tuesday = group.Any(d => d.Key == DayOfWeek.Tuesday),
                    Wednesday = group.Any(d => d.Key == DayOfWeek.Wednesday),
                    Thursday = group.Any(d => d.Key == DayOfWeek.Thursday),
                    Friday = group.Any(d => d.Key == DayOfWeek.Friday),
                    Saturday = group.Any(d => d.Key == DayOfWeek.Saturday),
                    Sunday = group.Any(d => d.Key == DayOfWeek.Sunday)
                };
                _context.Schedules.Add(schedule);
            }

            await _context.SaveChangesAsync();
            return RedirectToPage("/Admin/Index"); // Na razie wracamy do panelu
        }
    }
}