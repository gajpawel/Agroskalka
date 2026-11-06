using Agroskalka.Data;
using Agroskalka.Models;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Agroskalka.Pages;

public class HoursModel : PageModel
{
    private readonly AppDbContext _context;

    public HoursModel(AppDbContext context)
    {
        _context = context;
    }

    public List<Schedule> ActiveSchedules { get; set; } = new();

    public async Task OnGetAsync()
    {
        var now = DateTime.UtcNow;
        
        ActiveSchedules = await _context.Schedules
            .Where(s => s.ValidFrom <= now && s.ValidTo >= now)
            .ToListAsync();
    }
    
    public string GetHoursForDay(Func<Schedule, bool> daySelector)
    {
        var schedule = ActiveSchedules.FirstOrDefault(daySelector);
        if (schedule != null)
        {
            return $"{schedule.OpenTime:HH:mm} - {schedule.CloseTime:HH:mm}";
        }
        return "Zamknięte";
    }
}