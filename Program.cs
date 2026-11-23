using Agroskalka.Data;
using Agroskalka.Models;
using Agroskalka.Services.Captcha;
using Microsoft.AspNetCore.Identity;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Stripe;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;

DotNetEnv.Env.Load();
StripeConfiguration.ApiKey = Environment.GetEnvironmentVariable("STRIPE_SECRET_KEY");

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

builder.Services
    .AddOptions<CaptchaOptions>()
    .Bind(builder.Configuration.GetSection(CaptchaOptions.SectionName));
builder.Services.AddHttpClient<ICaptchaService, TurnstileCaptchaService>();

// ASP.NET Identity
builder.Services.AddIdentity<User, Role>(o =>
{
    o.User.RequireUniqueEmail = true;
    o.Password.RequiredLength = 6;
    o.Password.RequireNonAlphanumeric = false;
    o.Lockout.MaxFailedAccessAttempts = 5;
    o.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
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

// Ustawianie ciasteczka po logowaniu przez OICD
builder.Services.AddScoped<IUserClaimsPrincipalFactory<User>, AppClaimsPrincipalFactory>();

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

// Mechanizm Rate Limiting

builder.Services.AddRateLimiter(o =>
{
    o.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    // Logowanie / rejestracja: 5 prób na minutę na IP (tylko POST)
    o.AddPolicy("auth", ctx =>
        ctx.Request.Method == HttpMethods.Post
            ? RateLimitPartition.GetFixedWindowLimiter(
                ctx.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = 5,
                    Window = TimeSpan.FromMinutes(1),
                    QueueLimit = 0
                })
            : RateLimitPartition.GetNoLimiter("get"));

    // Globalny limit dla całej aplikacji: 100 żądań/min na IP
    o.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(ctx =>
        RateLimitPartition.GetFixedWindowLimiter(
            ctx.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 100,
                Window = TimeSpan.FromMinutes(1)
            }));
});

builder.Services.AddControllers();

// Logowanie OIDC (Keycloak)
builder.Services.AddAuthentication()
    .AddOpenIdConnect("Keycloak", "Keycloak", options =>
    {
        options.SignInScheme = IdentityConstants.ExternalScheme;
        options.Authority = Environment.GetEnvironmentVariable("OIDC_AUTHORITY")
                            ?? "http://localhost:8080/realms/agroskalka";
        options.ClientId = Environment.GetEnvironmentVariable("OIDC_CLIENT_ID")
                           ?? "agroskalka-web";
        options.ClientSecret = Environment.GetEnvironmentVariable("OIDC_CLIENT_SECRET");

        options.ResponseType = "code";
        options.SaveTokens = true;
        options.GetClaimsFromUserInfoEndpoint = true;
        options.RequireHttpsMetadata = !builder.Environment.IsDevelopment();

        options.Scope.Clear();
        options.Scope.Add("openid");
        options.Scope.Add("profile");
        options.Scope.Add("email");

        options.Events = new OpenIdConnectEvents
        {
            OnRedirectToIdentityProviderForSignOut = context =>
            {
                var idToken = context.Properties?.GetTokenValue("id_token");
                if (!string.IsNullOrEmpty(idToken))
                {
                    context.ProtocolMessage.IdTokenHint = idToken;
                }

                return Task.CompletedTask;
            }
        };
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

app.UseRateLimiter();
// Uwierzytelnianie musi by� przed autoryzacj�
app.UseAuthentication();
app.UseAuthorization();

app.MapRazorPages();
app.MapDefaultControllerRoute(); // je�li u�ywasz kontroler�w MVC obok Razor Pages
app.MapControllers();

app.Run();