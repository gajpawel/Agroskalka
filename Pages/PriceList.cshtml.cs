using Agroskalka.Data;
using Agroskalka.Models;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Agroskalka.Pages;

public class PriceListModel : PageModel
{
    private readonly AppDbContext _context;

    public PriceListModel(AppDbContext context)
    {
        _context = context;
    }

    public Tariff? Tariff { get; set; }

    public async Task OnGetAsync()
    {
        var today = DateTime.UtcNow.Date;

        Tariff = await _context.Tariffs
            .Where(t => t.IsActive &&
                        t.ValidFrom.Date <= today &&
                        t.ValidTo.Date >= today)
            .OrderByDescending(t => t.ValidFrom)
            .FirstOrDefaultAsync();
    }
}