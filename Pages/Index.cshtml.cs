using Agroskalka.Data;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Agroskalka.Pages;

public class IndexModel : PageModel
{
    private readonly AppDbContext _context;

    public IndexModel(AppDbContext context)
    {
        _context = context;
    }

    public string RolesText { get; set; } = string.Empty;

    public async Task OnGetAsync()
    {
        // Pobieramy role z bazy i ³¹czymy je przecinkiem
        var roles = await _context.Roles
            .OrderBy(r => r.Id)
            .Select(r => r.Alias)
            .ToListAsync();

        RolesText = $"*Test bazy* Role w systemie: {string.Join(", ", roles)}";
    }
}