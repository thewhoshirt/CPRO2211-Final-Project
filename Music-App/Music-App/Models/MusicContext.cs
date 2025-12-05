using Microsoft.EntityFrameworkCore; // Import EntityFrameworkCore so we can use MVC operations
/*
 * Add the database context to the models namespace so it can be accessed any time the model is accessed
 */
namespace Music_App.Models
{
    /*
     * Create the context
     */
    public class MusicContext : DbContext
    {
        public MusicContext(DbContextOptions<MusicContext> options) : base(options) {
        }
        /*
         * Getters and setters for the playlist and music entities
         */
        public DbSet<Playlist> Playlists { get; set; }
        public DbSet<Music> Musics { get; set; }
        /*
         * Ensure that the relationship between the models is valid when creating the database
         */
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Playlist>()
                .HasMany(p => p.MusicList)
                .WithMany(m => m.Playlists);
        }
    }
}
