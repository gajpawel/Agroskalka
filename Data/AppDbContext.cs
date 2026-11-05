using Agroskalka.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;

namespace Agroskalka.Data;

public class AppDbContext : IdentityDbContext<User, Role, int>
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }
    public DbSet<Tariff> Tariffs => Set<Tariff>();
    public DbSet<Schedule> Schedules => Set<Schedule>();
    public DbSet<Ticket> Tickets => Set<Ticket>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Precyzja dla kolumn finansowych/kwotowych
        modelBuilder.Entity<Tariff>()
            .Property(t => t.KidPrice).HasPrecision(10, 2);
        modelBuilder.Entity<Tariff>()
            .Property(t => t.AdultPrice).HasPrecision(10, 2);
        modelBuilder.Entity<Tariff>()
            .Property(t => t.SeniorPrice).HasPrecision(10, 2);
        modelBuilder.Entity<Tariff>()
            .Property(t => t.TrainPrice).HasPrecision(10, 2);

        modelBuilder.Entity<Ticket>()
            .Property(t => t.Price).HasPrecision(10, 2);

        // Relacja User -> Role - asp.net realizuje sam

        // Relacja Ticket -> User
        modelBuilder.Entity<Ticket>()
            .HasOne(t => t.User)
            .WithMany(u => u.Tickets)
            .HasForeignKey(t => t.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        // Relacja Ticket -> Tariff
        modelBuilder.Entity<Ticket>()
            .HasOne(t => t.Tariff)
            .WithMany(tf => tf.Tickets)
            .HasForeignKey(t => t.TariffId)
            .OnDelete(DeleteBehavior.Restrict);

    }
}