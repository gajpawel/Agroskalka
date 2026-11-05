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

    public Schedule? Schedule { get; set; }

    public async Task OnGetAsync()
    {
        Schedule = await _context.Schedules.FirstOrDefaultAsync();
    }
}