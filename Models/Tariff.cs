using System.Net.Sockets;

namespace Agroskalka.Models;

public class Tariff
{
    public int Id { get; set; }
    public DateTime ValidFrom { get; set; }
    public DateTime ValidTo { get; set; }
    public bool IsActive { get; set; }

    public decimal KidPrice { get; set; }
    public decimal AdultPrice { get; set; }
    public decimal SeniorPrice { get; set; }
    public decimal TrainPrice { get; set; } // na razie nie używamy

    public ICollection<Ticket> Tickets { get; set; } = new List<Ticket>();
}