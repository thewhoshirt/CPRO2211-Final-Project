using Microsoft.EntityFrameworkCore;

namespace Music_App.Models
{
    public class MusicContext : DbContext
    {
        public MusicContext(DbContextOptions<MusicContext> options) : base(options)
        {
        }
        public DbSet<Music> Musics { get; set; }


    }
}
