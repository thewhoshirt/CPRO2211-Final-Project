using Microsoft.EntityFrameworkCore;
using Music_App.Models;

namespace Music_App.Models
{
    public class MusicContext : DbContext
    {
        public MusicContext(DbContextOptions<MusicContext> options)
            : base(options)
        {
        }
        public DbSet<Playlist> Playlists { get; set; }
        public DbSet<Music> Musics { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Playlist>()
                .HasMany(p => p.MusicList)
                .WithMany(m => m.Playlists);
        }
    }
}
