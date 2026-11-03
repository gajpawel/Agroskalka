using System.Collections.Generic;
using System.Reflection.Emit;
using Agroskalka.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Agroskalka.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<Role> Roles => Set<Role>();
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

        // Relacja User -> Role
        modelBuilder.Entity<User>()
            .HasOne(u => u.Role)
            .WithMany(r => r.Users)
            .HasForeignKey(u => u.RoleId)
            .OnDelete(DeleteBehavior.Restrict);

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

        modelBuilder.Entity<Role>().HasData(
            new Role
            {
                Id = 1,
                Alias = "Administrator",
                Name = "admin"
            },
            new Role
            {
                Id = 2,
                Alias = "Klient",
                Name = "client"
            }
        );

        var hasher = new PasswordHasher<User>();
        var adminUser = new User
        {
            Id = 1,
            RoleId = 1,
            Name = "Główny",
            Surname = "Administrator",
            Email = "account@example.com",
            IsActive = true,
            IsApproved = true
        };
        adminUser.Password = hasher.HashPassword(adminUser, "Haslo1");

        modelBuilder.Entity<User>().HasData(adminUser);
    }
}