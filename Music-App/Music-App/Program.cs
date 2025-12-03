using Microsoft.EntityFrameworkCore;
using Music_App.Models;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
            .AllowAnyMethod()
            .AllowAnyHeader();
    });
});

builder.Services.AddControllers();
// Add services to the container.
builder.Services.AddControllersWithViews();

builder.Services.AddDbContext<MusicContext>(options => options.UseSqlServer(builder.Configuration.GetConnectionString("MusicContext")));

var app = builder.Build();

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseCors("AllowAll");
app.UseRouting();
app.UseAuthorization();
app.MapControllers();
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Musics}/{action=Index}/{id?}");


app.Run();
