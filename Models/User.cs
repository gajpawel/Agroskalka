using Microsoft.AspNetCore.Identity;

namespace Agroskalka.Models;

public class User : IdentityUser<int>
{
    public string Name { get; set; } = string.Empty;
    public string Surname { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public bool IsApproved { get; set; } = true;

    public ICollection<Ticket> Tickets { get; set; } = new List<Ticket>();
}