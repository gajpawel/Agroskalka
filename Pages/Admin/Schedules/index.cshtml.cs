using Agroskalka.Data;
using Agroskalka.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Agroskalka.Pages.Admin.Schedules
{
    public class IndexModel : PageModel
    {
        private readonly AppDbContext _context;

        public IndexModel(AppDbContext context)
        {
            _context = context;
        }

        // Pomocnicza klasa do grupowania rekordów na froncie
        public class ScheduleGroup
        {
            public DateTime ValidFrom { get; set; }
            public DateTime ValidTo { get; set; }
            public List<Schedule> Configs { get; set; } = new();
        }

        public List<ScheduleGroup> ScheduleGroups { get; set; } = new();

        public async Task OnGetAsync()
        {
            // Pobieramy wszystko sortując od najnowszego
            var allSchedules = await _context.Schedules
                .OrderByDescending(s => s.ValidFrom)
                .ToListAsync();

            // Grupujemy rekordy, które mają tę samą datę początkową
            ScheduleGroups = allSchedules
                .GroupBy(s => s.ValidFrom)
                .Select(g => new ScheduleGroup
                {
                    ValidFrom = g.Key,
                    ValidTo = g.First().ValidTo, // Wszystkie dni w grupie mają to samo ValidTo
                    Configs = g.ToList()
                })
                .ToList();
        }
        
        public async Task<IActionResult> OnPostDeleteAsync(DateTime date)
        {
            // Upewniamy się, że przysłana data jest traktowana jako UTC
            var utcDate = DateTime.SpecifyKind(date, DateTimeKind.Utc);

            // Pobieramy całą paczkę rekordów przypisaną do tego dnia
            var schedulesToDelete = await _context.Schedules
                .Where(s => s.ValidFrom == utcDate)
                .ToListAsync();

            if (!schedulesToDelete.Any()) return RedirectToPage();

            _context.Schedules.RemoveRange(schedulesToDelete);
            await _context.SaveChangesAsync();

            return RedirectToPage();
        }
    }
    
    
}