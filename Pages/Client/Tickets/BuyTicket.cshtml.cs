using Agroskalka.Data;
using Agroskalka.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Stripe.Checkout;
using System.Security.Claims;
using System.Security.Cryptography;

namespace Agroskalka.Pages.Tickets;

[Authorize]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public class BuyTicketModel : PageModel
{
    private readonly AppDbContext _context;

    public BuyTicketModel(AppDbContext context)
    {
        _context = context;
    }

    [BindProperty]
    public DateOnly SelectedDate { get; set; } = DateOnly.FromDateTime(DateTime.Today);

    [BindProperty]
    public int BabiesCount { get; set; }

    [BindProperty]
    public int KidsCount { get; set; }

    [BindProperty]
    public int AdultsCount { get; set; }

    [BindProperty]
    public int SeniorsCount { get; set; }

    public List<Tariff> ActiveTariffs { get; set; } = new();
    public List<Schedule> ActiveSchedules { get; set; } = new();

    public async Task OnGetAsync()
    {
        await LoadDataAsync();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        await LoadDataAsync();

        var selectedDateTime = SelectedDate.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);

        // 1. Weryfikacja harmonogramu
        var dayOfWeek = SelectedDate.DayOfWeek;
        var schedule = await _context.Schedules
            .FirstOrDefaultAsync(s =>
                s.ValidFrom <= selectedDateTime &&
                s.ValidTo >= selectedDateTime &&
                (
                    (dayOfWeek == DayOfWeek.Monday && s.Monday) ||
                    (dayOfWeek == DayOfWeek.Tuesday && s.Tuesday) ||
                    (dayOfWeek == DayOfWeek.Wednesday && s.Wednesday) ||
                    (dayOfWeek == DayOfWeek.Thursday && s.Thursday) ||
                    (dayOfWeek == DayOfWeek.Friday && s.Friday) ||
                    (dayOfWeek == DayOfWeek.Saturday && s.Saturday) ||
                    (dayOfWeek == DayOfWeek.Sunday && s.Sunday)
                ));

        if (schedule == null)
        {
            ModelState.AddModelError("SelectedDate", "W wybranym dniu park jest nieczynny.");
            return Page();
        }

        // 2. Weryfikacja taryfy
        var tariff = await _context.Tariffs
            .FirstOrDefaultAsync(t =>
                t.IsActive &&
                t.ValidFrom <= selectedDateTime &&
                t.ValidTo >= selectedDateTime);

        if (tariff == null)
        {
            ModelState.AddModelError("SelectedDate", "Brak aktywnego cennika na wybrany dzieñ.");
            return Page();
        }

        // 3. Walidacja liczby goœci i obliczenie kwoty
        decimal totalPrice = (KidsCount * tariff.KidPrice) +
                             (AdultsCount * tariff.AdultPrice) +
                             (SeniorsCount * tariff.SeniorPrice);

        if (KidsCount <= 0 && AdultsCount <= 0 && SeniorsCount <= 0)
        {
            ModelState.AddModelError("", "Musisz wybraæ przynajmniej jeden p³atny bilet.");
            return Page();
        }

        // 4. Wygenerowanie unikalnego kodu biletu
        string ticketCode;
        do
        {
            ticketCode = GenerateTicketCode();
        }
        while (await _context.Tickets.AnyAsync(t => t.Code == ticketCode));

        // 5. Zapis rekordu Ticket ze statusem IsPaid = false
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier)
                              ?? User.FindFirstValue("UserId"); // zabezpieczenie, jeœli u¿ywasz w³asnego claima

        if (!int.TryParse(userIdClaim, out int currentUserId))
        {
            return Challenge(); // Przekieruje do strony logowania, jeœli sesja wygas³a
        }

        var ticket = new Ticket
        {
            Code = ticketCode,
            TariffId = tariff.Id,
            UserId = currentUserId, // Docelowo z zalogowanego u¿ytkownika
            Price = totalPrice,
            Date = SelectedDate,
            KidsNumber = KidsCount,
            AdultsNumber = AdultsCount,
            SeniorsNumber = SeniorsCount,
            BabiesNumber = BabiesCount,
            IsPaid = false, // Oczekuje na p³atnoœæ
            IsChecked = false,
            HasDiscount = false
        };

        _context.Tickets.Add(ticket);
        await _context.SaveChangesAsync();

        // 6. Utworzenie sesji Stripe Checkout
        var domain = $"{Request.Scheme}://{Request.Host}";

        var options = new SessionCreateOptions
        {
            //PaymentMethodTypes = new List<string> { "card", "blik", "p24" },
            Mode = "payment",
            // Stripe sam podmieni {CHECKOUT_SESSION_ID} na losowy token sesji
            SuccessUrl = $"{domain}/Client/Tickets/PaymentResult?sessionId={{CHECKOUT_SESSION_ID}}",
            CancelUrl = $"{domain}/Client/Tickets/PaymentResult?sessionId={{CHECKOUT_SESSION_ID}}&canceled=true",
            Metadata = new Dictionary<string, string>
            {
                { "ticket_id", ticket.Id.ToString() },
                { "ticket_code", ticket.Code }
            },
            LineItems = new List<SessionLineItemOptions>
            {
                new SessionLineItemOptions
                {
                    PriceData = new SessionLineItemPriceDataOptions
                    {
                        Currency = "pln",
                        // Stripe operuje w groszach (long), dlatego mno¿ymy przez 100
                        UnitAmount = (long)(totalPrice * 100),
                        ProductData = new SessionLineItemPriceDataProductDataOptions
                        {
                            Name = $"Bilet wstêpu do Agroska³ki ({SelectedDate:dd.MM.yyyy})",
                            Description = $"Kod biletu: {ticket.Code} | Doroœli: {AdultsCount}, Dzieci: {KidsCount}, Seniorzy: {SeniorsCount}, Maluchy <90cm: {BabiesCount}"
                        }
                    },
                    Quantity = 1
                }
            }
        };

        var service = new SessionService();
        Session session = await service.CreateAsync(options);

        // Przekierowanie u¿ytkownika na bramkê p³atnoœci Stripe
        Response.Headers.Location = session.Url;
        return new StatusCodeResult(303);
    }

    private async Task LoadDataAsync()
    {
        ActiveTariffs = await _context.Tariffs.Where(t => t.IsActive).ToListAsync();
        ActiveSchedules = await _context.Schedules.ToListAsync();
    }

    private static string GenerateTicketCode()
    {
        const string chars = "ABCDEFGHJKMNPQRSTUVWXYZ23456789";
        string raw = RandomNumberGenerator.GetString(chars, 6);
        return $"{raw[..3]}-{raw[3..]}";
    }
}