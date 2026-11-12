using Agroskalka.Data;
using Agroskalka.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;

namespace Agroskalka.Pages.Tickets;

[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)] // Wymusza odœwie¿enie przy cofaniu w przegl¹darce
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
        // Zawsze ³adujemy dane na wypadek, gdyby walidacja nie przesz³a i trzeba by³o ponownie wyrenderowaæ stronê
        await LoadDataAsync();

        var selectedDateTime = SelectedDate.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);

        // 1. Weryfikacja harmonogramu (Schedule)
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

        // 2. Weryfikacja taryfy (Tariff)
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

        // 3. Walidacja liczby goœci
        decimal totalPrice = (KidsCount * tariff.KidPrice) +
                             (AdultsCount * tariff.AdultPrice) +
                             (SeniorsCount * tariff.SeniorPrice);

        if (KidsCount <= 0 && AdultsCount <= 0 && SeniorsCount <= 0)
        {
            ModelState.AddModelError("", "Musisz wybraæ przynajmniej jeden p³atny bilet.");
            return Page();
        }

        string ticketCode;
        do
        {
            ticketCode = GenerateTicketCode();
        }
        while (await _context.Tickets.AnyAsync(t => t.Code == ticketCode));

        // 4. Utworzenie i zapis biletu
        var ticket = new Ticket
        {
            Code = ticketCode,
            TariffId = tariff.Id,
            UserId = 2, // Docelowo z zalogowanego u¿ytkownika
            Price = totalPrice,
            Date = SelectedDate,
            KidsNumber = KidsCount,
            AdultsNumber = AdultsCount,
            SeniorsNumber = SeniorsCount,
            BabiesNumber = BabiesCount,
            IsPaid = true,
            IsChecked = false,
            HasDiscount = false
        };

        _context.Tickets.Add(ticket);
        await _context.SaveChangesAsync();

        // Przekierowanie z odpowiednim nag³ówkiem cache, aby Wstecz wymusza³ prze³adowanie/ponowne wys³anie
        return RedirectToPage("/Client/Index");
    }

    private async Task LoadDataAsync()
    {
        ActiveTariffs = await _context.Tariffs
            .Where(t => t.IsActive)
            .ToListAsync();

        ActiveSchedules = await _context.Schedules
            .ToListAsync();
    }

    private static string GenerateTicketCode()
    {
        const string chars = "ABCDEFGHJKMNPQRSTUVWXYZ23456789";
        string raw = RandomNumberGenerator.GetString(chars, 6);
        return $"{raw[..3]}-{raw[3..]}"; // np. X3K-P9M
    }
}