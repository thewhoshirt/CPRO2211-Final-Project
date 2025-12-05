using System.ComponentModel.DataAnnotations; // Import DataAnnotations so that validations can be used
/*
 * Add the playlist model the models namespace
 */
namespace Music_App.Models
{
    /*
     * Create the playlist model
     */
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