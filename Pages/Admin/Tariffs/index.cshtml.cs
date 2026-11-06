using Agroskalka.Data;
using Agroskalka.Models;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Agroskalka.Pages.Admin.Tariffs
{
    public class IndexModel : PageModel
    {
        private readonly AppDbContext _context;

        public IndexModel(AppDbContext context)
        {
            _context = context;
        }

        public IList<Tariff> Tariffs { get; set; } = default!;

        public async Task OnGetAsync()
        {
            Tariffs = await _context.Tariffs
                .OrderByDescending(t => t.ValidFrom)
                .ToListAsync();
        }
    }
}