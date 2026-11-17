using System.Security.Claims;
using Agroskalka.Data;
using Agroskalka.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Stripe.Checkout;

namespace Agroskalka.Pages.Client.Tickets;

[Authorize] // Wymaga zalogowania
public class PaymentResultModel : PageModel
{
    private readonly AppDbContext _context;

    public PaymentResultModel(AppDbContext context)
    {
        _context = context;
    }

    public Ticket? Ticket { get; set; }
    public bool IsSuccess { get; set; }

    public async Task<IActionResult> OnGetAsync(string? sessionId, bool canceled = false)
    {
        // 1. Brak tokenu sesji -> odrzucamy
        if (string.IsNullOrWhiteSpace(sessionId))
        {
            return RedirectToPage("/Client/Index");
        }

        // 2. Pobranie ID zalogowanego u¿ytkownika z Claims
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(userIdClaim, out int currentUserId))
        {
            return Forbid();
        }

        try
        {
            // 3. Weryfikacja sesji bezpoœrednio w API Stripe
            var sessionService = new SessionService();
            var session = await sessionService.GetAsync(sessionId);

            if (session == null ||
                !session.Metadata.TryGetValue("ticket_id", out var ticketIdStr) ||
                !int.TryParse(ticketIdStr, out int ticketId))
            {
                return NotFound();
            }

            // 4. Pobranie biletu TYLKO jeœli nale¿y do zalogowanego u¿ytkownika
            Ticket = await _context.Tickets
                .FirstOrDefaultAsync(t => t.Id == ticketId && t.UserId == currentUserId);

            if (Ticket == null)
            {
                // Bilet nie istnieje lub nale¿y do kogoœ innego
                return Forbid();
            }

            // Prawdziwy status p³atnoœci pochodzi ze Stripe / bazy, a nie z parametru w URL
            IsSuccess = !canceled && (session.PaymentStatus == "paid" || Ticket.IsPaid);

            return Page();
        }
        catch
        {
            // Niepoprawny lub sfa³szowany sessionId
            return RedirectToPage("/Client/Index");
        }
    }
}