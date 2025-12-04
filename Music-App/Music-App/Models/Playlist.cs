using System.ComponentModel.DataAnnotations;

namespace Music_App.Models
{
    public class Playlist
    {
        [Key]
        [Required]
        public int PlaylistId { get; set; }

        [Required]
        public string PlaylistName { get; set; }

        public ICollection<Music> MusicList { get; set; } = new List<Music>();

    }
}
