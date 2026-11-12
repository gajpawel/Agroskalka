namespace Agroskalka.Models;

public class Ticket
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public int TariffId { get; set; }
    public int UserId { get; set; }

    public decimal Price { get; set; }
    public int KidsNumber { get; set; }
    public int AdultsNumber { get; set; }
    public int SeniorsNumber { get; set; }
    public int BabiesNumber { get; set; }

    public DateOnly Date { get; set; }
    public string? FilePath { get; set; }

    public bool HasDiscount { get; set; }
    public bool IsPaid { get; set; }
    public bool IsChecked { get; set; }

    public Tariff Tariff { get; set; } = null!;
    public User User { get; set; } = null!;
}