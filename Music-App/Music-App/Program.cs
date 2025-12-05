/*
 * Import EntityFrameworkCore so we can use MVC functions
 * and import the model(s) so that the program can create/use the database
 */
using Microsoft.EntityFrameworkCore;
using Music_App.Models;
/*
 * Create the builder and configure CORS so multiple pages can be used
 * (this was in preparation for the url streaming but we didn't get that far)
 */
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
/*
 * Add services to the container.
 */
builder.Services.AddControllersWithViews();
builder.Services.AddDbContext<MusicContext>(options => options.UseSqlServer(builder.Configuration.GetConnectionString("MusicContext")));
builder.Services.AddControllers();
/*
 * Create the app and configure all of the dependencies and routing
 */
var app = builder.Build();
app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseCors("AllowAll");
app.UseRouting();
app.UseAuthorization();
app.MapControllers();
/*
 * Set the default route to be the index page of the Musics controller
 */
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Musics}/{action=Index}/{id?}");
app.Run(); // run the app