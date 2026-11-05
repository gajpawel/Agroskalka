using Microsoft.AspNetCore.Identity;

namespace Agroskalka.Models;

public class Role : IdentityRole<int>
{
    public string Alias { get; set; } = string.Empty; // nazwa do wyświetlania, np. "Klient"
}