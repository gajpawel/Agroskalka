using Agroskalka.Data;
using Agroskalka.Models;
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
    }
}