using System.ComponentModel.DataAnnotations;

namespace Music_App.Models
{
    public class Music
    {
        [Key]
        [Required]
        public int TrackId { get; set; }

        [Required]
        [RegularExpression(@"(?i).*\.mp3$", ErrorMessage = "TrackFile must be a valid .mp3 file")]
        public string TrackFile { get; set; }

        [Required]
        public string TrackTitle { get; set; }

        [Required]
        public string TrackArtist { get; set; }

        [Required]
        public double TrackLength { get; set; }

        public ICollection<Playlist> Playlists { get; set; } = new List<Playlist>();
    }
}
