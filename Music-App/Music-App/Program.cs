using Microsoft.EntityFrameworkCore;
using Music_App.Models;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

builder.Services.AddDbContext<MusicContext>(options => options.UseSqlServer(builder.Configuration.GetConnectionString("MusicContext")));

var app = builder.Build();

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Musics}/{action=Database}/{id?}");

app.Run();
