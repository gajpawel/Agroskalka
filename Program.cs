using Agroskalka.Data;
using Microsoft.EntityFrameworkCore;
using Agroskalka.Models;
using Microsoft.AspNetCore.Identity;

DotNetEnv.Env.Load();

var builder = WebApplication.CreateBuilder(args);

string connectionString;

var dbHost = Environment.GetEnvironmentVariable("DB_HOST");
if (!string.IsNullOrEmpty(dbHost))
{
    connectionString = $"Host={dbHost};" +
                       $"Port={Environment.GetEnvironmentVariable("DB_PORT") ?? "5432"};" +
                       $"Database={Environment.GetEnvironmentVariable("DB_NAME")};" +
                       $"Username={Environment.GetEnvironmentVariable("DB_USER")};" +
                       $"Password={Environment.GetEnvironmentVariable("DB_PASSWORD")}";
}
else
{
    connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
        ?? throw new InvalidOperationException("Brak konfiguracji po��czenia z baz� danych (sprawd� .env lub appsettings.json).");
}

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(connectionString)
           .UseSnakeCaseNamingConvention());


// ASP.NET Identity
builder.Services.AddIdentity<User, Role>(o =>
{
    o.User.RequireUniqueEmail = true;
    o.Password.RequiredLength = 6;
    o.Password.RequireNonAlphanumeric = false;
    o.Lockout.MaxFailedAccessAttempts = 5;
})
.AddEntityFrameworkStores<AppDbContext>()
.AddDefaultTokenProviders()
.AddDefaultUI();

// Konfiguracja ciasteczek logowania
builder.Services.ConfigureApplicationCookie(o =>
{
    o.LoginPath = "/Identity/Account/Login";
    o.AccessDeniedPath = "/Identity/Account/AccessDenied";
    o.ExpireTimeSpan = TimeSpan.FromHours(8);
    o.SlidingExpiration = true;
});

// Autoryzacja
builder.Services.AddAuthorization(o =>
{
    o.AddPolicy("AdminOnly", p => p.RequireRole("Admin"));
    o.AddPolicy("ClientOnly", p => p.RequireRole("Client"));
});

builder.Services.AddControllersWithViews();

// Zabezpieczenie folder�w Razor Pages
builder.Services.AddRazorPages(o =>
{
    o.Conventions.AuthorizeFolder("/Admin", "AdminOnly");
    o.Conventions.AuthorizeFolder("/Client", "ClientOnly");
});


var app = builder.Build();

// Seeder - tworzenie domy�lnych r�l i administratora
using (var scope = app.Services.CreateScope())
{
    var roles = scope.ServiceProvider.GetRequiredService<RoleManager<Role>>();
    var users = scope.ServiceProvider.GetRequiredService<UserManager<User>>();

    var defaultRoles = new[]
    {
        (Name: "Admin", Alias: "Administrator"),
        (Name: "Client", Alias: "Klient")
    };

    foreach (var (name, alias) in defaultRoles)
    {
        if (!await roles.RoleExistsAsync(name))
        {
            await roles.CreateAsync(new Role
            {
                Name = name,
                Alias = alias
            });
        }
    }

    const string adminEmail = "account@example.com";

    if (await users.FindByEmailAsync(adminEmail) == null)
    {
        var admin = new User
        {
            UserName = adminEmail,
            Email = adminEmail,
            EmailConfirmed = true,
            Name = "Admin",
            Surname = "Admin",
            IsActive = true,
            IsApproved = true
        };

        var result = await users.CreateAsync(admin, "Haslo1");

        if (result.Succeeded)
        {
            await users.AddToRoleAsync(admin, "Admin");
        }
        else
        {
            foreach (var error in result.Errors)
            {
                Console.WriteLine($"Admin creation error: {error.Description}");
            }
        }
    }
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

// Uwierzytelnianie musi by� przed autoryzacj�
app.UseAuthentication();
app.UseAuthorization();

app.MapRazorPages();
app.MapDefaultControllerRoute(); // je�li u�ywasz kontroler�w MVC obok Razor Pages

app.Run();