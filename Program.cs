using Agroskalka.Data;
using Microsoft.EntityFrameworkCore;

DotNetEnv.Env.Load();

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? $"Host={Environment.GetEnvironmentVariable("DB_HOST")};" +
       $"Port={Environment.GetEnvironmentVariable("DB_PORT") ?? "5432"};" +
       $"Database={Environment.GetEnvironmentVariable("DB_NAME")};" +
       $"Username={Environment.GetEnvironmentVariable("DB_USER")};" +
       $"Password={Environment.GetEnvironmentVariable("DB_PASSWORD")}";

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(connectionString)
           .UseSnakeCaseNamingConvention());

builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapRazorPages();
app.MapDefaultControllerRoute(); // jeœli u¿ywasz kontrolerów MVC obok Razor Pages

app.Run();