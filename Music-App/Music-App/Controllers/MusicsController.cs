using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Music_App.Models;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using NuGet.DependencyResolver;


namespace Music_App.Controllers
{
    public class MusicsController : Controller
    {
        private static WaveOutEvent output;
        private static AudioFileReader audioFile;
        private static int sampleRate;
        public static TimeSpan duration;
        private static bool isAudioPlaying = false;
        private readonly MusicContext _context;
        // makes the max a file can be to 3mb 
        private long maxFileSize = 1024 * 1024 * 3; //  = 3 MB
        private readonly IConfiguration _config;

        private static List<string> playlistFiles = new List<string>();
        private static List<Music> currentPlaylist = new List<Music>();
        private static int currentSongIndex = 0;



        public MusicsController(MusicContext context, IConfiguration config)
        {
            _context = context;
            _config = config;
        }

        // GET: Musics
        public async Task<IActionResult> Index()
        {
            return _context.Musics != null
                ? View(await _context.Musics.ToListAsync())
                : Problem("Entity set 'MusicContext.Musics'  is null.");
        }

        // GET: Musics/Database
        public async Task<IActionResult> Database(string searchQuery)
        {
            var musics = _context.Musics.AsQueryable();
            if (!string.IsNullOrEmpty(searchQuery))
            {
                musics = musics.Where(m => m.TrackTitle.Contains(searchQuery) || m.TrackArtist.Contains(searchQuery));
            }
            ViewData["SearchQuery"] = searchQuery;
            return View(await musics.ToListAsync());
        }



        // GET: Musics/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Musics/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            [Bind("TrackId,TrackFile,TrackTitle,TrackArtist,TrackLength")] Music music)
        {
            if (ModelState.IsValid)
            {
                // Get the base path for file storage from configuration or use default
                var basePath = _config.GetValue<string>("FileStorage")
                    ?? Path.Combine(Directory.GetCurrentDirectory(), "FileStorage");

                // Ensure the base directory exists
                var musicDirectory = Path.Combine(basePath, "MusicFiles");


                // Create the directory if it doesn't exist
                Directory.CreateDirectory(musicDirectory);

                // Combine base path with the provided file name
                var fileName = TrimPath(music.TrackFile ?? string.Empty);

                // Ensure the file name is valid
                var filePath = Path.Combine(musicDirectory, fileName);

                // Save the file path in the database column
                music.TrackFile = filePath;

                /*
                 * Get the track length data from the file
                 */
                using (var reader = new AudioFileReader(filePath))
                {
                    duration = reader.TotalTime; // get total time in seconds
                    Console.WriteLine($"Track Length: {duration.TotalSeconds} seconds");
                }
                music.TrackLength = Math.Round(duration.TotalMinutes, 2); // save the time in minutes to 2 decimal places

                // save the music entry to the database
                _context.Add(music);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Database));
            }

            return View(music);
        }

        // GET: Musics/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null || _context.Musics == null)
            {
                return NotFound();
            }

            var music = await _context.Musics.FindAsync(id);
            if (music == null)
            {
                return NotFound();
            }

            return View(music);
        }

        // POST: Musics/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id,
            [Bind("TrackId,TrackFile,TrackTitle,TrackArtist,TrackLength")] Music music)


        {
            if (id != music.TrackId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {

                    // Get the base path for file storage from configuration or use default
                    var basePath = _config.GetValue<string>("FileStorage")
                        ?? Path.Combine(Directory.GetCurrentDirectory(), "FileStorage");

                    // Ensure the base directory exists
                    var musicDirectory = Path.Combine(basePath, "MusicFiles");


                    // Create the directory if it doesn't exist
                    Directory.CreateDirectory(musicDirectory);

                    // Combine base path with the provided file name
                    var fileName = TrimPath(music.TrackFile ?? string.Empty);

                    // Ensure the file name is valid
                    var filePath = Path.Combine(musicDirectory, fileName);

                    // Save the file path in the database column
                    music.TrackFile = filePath;

                    /*
                     * Get the track length data from the file
                     */
                    using (var reader = new AudioFileReader(filePath))
                    {
                        duration = reader.TotalTime; // get total time in seconds
                        Console.WriteLine($"Track Length: {duration.TotalSeconds} seconds");
                    }
                    music.TrackLength = Math.Round(duration.TotalMinutes, 2); // save the time in minutes to 2 decimal places
                    _context.Update(music);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!MusicExists(music.TrackId))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }

                return RedirectToAction(nameof(Database));
            }

            return View(music);
        }

        // GET: Musics/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null || _context.Musics == null)
            {
                return NotFound();
            }

            var music = await _context.Musics
                .FirstOrDefaultAsync(m => m.TrackId == id);
            if (music == null)
            {
                return NotFound();
            }

            return View(music);
        }

        // POST: Musics/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            if (_context.Musics == null)
            {
                return Problem("Entity set 'MusicContext.Musics'  is null.");
            }

            var music = await _context.Musics.FindAsync(id);
            if (music != null)
            {
                _context.Musics.Remove(music);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Database));
        }

        private bool MusicExists(int id)
        {
            return (_context.Musics?.Any(e => e.TrackId == id)).GetValueOrDefault();
        }

        [HttpPost("play/{trackId}")]
        public IActionResult Play(int trackId)
        {
            if (output == null)
            {
                output = new WaveOutEvent();
                output.PlaybackStopped += OnPlaybackStopped;
                Console.WriteLine("Initialized output object.");
            }

            if (audioFile != null)
            {
                audioFile.Dispose();  // Dispose of the previous file if any
            }

            var track = _context.Musics.FirstOrDefault(m => m.TrackId == trackId);
            if (track == null) return NotFound("Track not found.");

            var audioPath = track.TrackFile?.Trim().Trim('"');
            if (string.IsNullOrWhiteSpace(audioPath) || !System.IO.File.Exists(audioPath))
                return NotFound("Audio file path is missing or file not found.");

            audioFile = new AudioFileReader(audioPath);
            using (var reader = new Mp3FileReader(audioPath))
            {
                sampleRate = reader.Mp3WaveFormat.SampleRate;
                Console.WriteLine("Sample Rate: " + sampleRate);
            }
            output.Init(audioFile);
            output.Play();
            isAudioPlaying = true;
            audioFile.Volume = 0.5f; // set volume to 50% when play is clicked
            Console.WriteLine("Audio started playing.");
            return Ok(new { message = "Audio is playing", trackTitle = track.TrackTitle, trackArtist = track.TrackArtist });
        }

        [HttpPost("stop")]
        public IActionResult Stop()
        {
            Console.WriteLine("Stop button pressed.");

            if (output == null || !isAudioPlaying)
            {
                Console.WriteLine("No audio is currently playing.");
                return BadRequest("No audio is currently playing.");
            }

            output.Stop();
            audioFile = null;
            playlistFiles.Clear();
            currentPlaylist.Clear();
            currentSongIndex = 0;
            isAudioPlaying = false;
            Console.WriteLine("Audio stopped successfully.");
            return Ok("Audio stopped");
        }

        [HttpPost("pause")]
        public IActionResult Pause()
        {
            Console.WriteLine("Puase button pressed.");

            if (output == null)
            {
                Console.WriteLine("No audio is currently playing.");
                return BadRequest("No audio is currently playing.");
            }

            if (!isAudioPlaying)
            {
                output.Play();
                isAudioPlaying = true;
                Console.WriteLine("Audio is playing.");
                return Ok("Audio resumed");
            }
            else
            {
                output.Pause();
                isAudioPlaying = false;
                Console.WriteLine("Audio paused successfully.");
                return Ok("Audio paused");
            }
        }

        [HttpPost("rewind")]
        public IActionResult Rewind()
        {
            Console.WriteLine("Rewind button pressed.");

            if (output == null || !isAudioPlaying)
            {
                Console.WriteLine("No audio is currently playing.");
                return BadRequest("No audio is currently playing.");
            }
            long seconds = 10;
            audioFile.Position -= (long)((seconds * 10) * sampleRate); // rewind ~10 seconds based on sample rate (actually 13?)
            Console.WriteLine("Rewinded successfully.");
            return Ok("Audio rewound");
        }

        [HttpPost("forward")]
        public IActionResult Forward()
        {
            Console.WriteLine("Forward button pressed.");

            if (output == null || !isAudioPlaying)
            {
                Console.WriteLine("No audio is currently playing.");
                return BadRequest("No audio is currently playing.");
            }
            int seconds = 10;
            audioFile.Position += (long)((seconds * 10) * sampleRate); // fast-forward ~10 seconds based on sample rate (actually 13?)
            Console.WriteLine("Fast-forwarded successfully.");
            return Ok("Audio fast-forwarded");
        }

        
        /// <summary>
        /// Raise the volume of the audioFileReader (open audio file) when the Vol up button is pressed
        /// ranges from 0.0f to 1.0f, 0.1f (10%) increments were chosen as a sane default but anything less than 1.0f will work
        /// </summary>
        /// <returns>Ok when volume is raised or BadRequest when volume has reached the limit</returns>
        [HttpPost("up")]
        public IActionResult Up()
        {
            Console.WriteLine("Volume up button pressed.");
            if (output == null || !isAudioPlaying)
            {
                Console.WriteLine("No audio is currently playing.");
                return BadRequest("No audio is currently playing.");
            }

            if (audioFile.Volume >= 1.0f)
            {
                audioFile.Volume = 1.0f; // if the volume goes above 100 set to 100%
                return BadRequest("Volume cannot go above 100");
            }
            audioFile.Volume += 1 / 10.0f; // increase volume by 0.1
            Console.WriteLine("Volume raised successfully.");
            return Ok("Volume raised");
        }
        
        /// <summary>
        /// Decrease the volume of the audioFileReader (open audio file) when the Vol down button is pressed
        /// ranges from 0.0f to 1.0f, 0.1f (10%) increments were chosen as a sane default but anything less than 1.0f will work
        /// </summary>
        /// <returns>Ok when volume is lowered or BadRequest when volume has reached the limit</returns>
        [HttpPost("down")]
        public IActionResult Down()
        {
            Console.WriteLine("Volume down button pressed.");
            if (output == null || !isAudioPlaying)
            {
                Console.WriteLine("No audio is currently playing.");
                return BadRequest("No audio is currently playing.");
            }

            if (audioFile.Volume <= 0.0f)
            {
                audioFile.Volume = 0.0f; // if the volume goes below 0 set to 0%
                return BadRequest("Volume cannot go below 0");
            }
            audioFile.Volume -= 1 / 10.0f; // decrease volume by 0.1
            Console.WriteLine("Volume lowered successfully.");
            return Ok("Volume raised");
        }
        
        private void OnPlaybackStopped(object sender, StoppedEventArgs args)
        {
            output?.Dispose();
            audioFile?.Dispose();
            output = null;
            audioFile = null;
            isAudioPlaying = false;
            Console.WriteLine("Playback stopped, resources disposed.");
        }

        // added to trim path inputs from user so that they read correctly 
        private static string TrimPath(string path)
        {
            if (string.IsNullOrWhiteSpace(path)) return path;
            path = path.Trim();
            if (path.Length >= 2 && path.StartsWith("\"") && path.EndsWith("\""))
                path = path.Substring(1, path.Length - 2);
            return path.Trim();
        }

        // sets the time progress bar
        public ActionResult GetPlaybackProgress()
        {    

            if (audioFile != null)
            {
                // Get current time in seconds and total duration in seconds
                double currentTime = audioFile.CurrentTime.TotalSeconds;
                double totalTime = audioFile.TotalTime.TotalSeconds;
                // Calculate percentage 
                int percentage = totalTime > 0 ? (int)((currentTime / totalTime) * 100) : 0;

                if (playlistFiles.Count > 0)
                {
                    var TrackTitle = currentPlaylist[currentSongIndex].TrackTitle;
                    var TrackArtist = currentPlaylist[currentSongIndex].TrackArtist;
                    return Json(new { currentTime = currentTime, totalTime = totalTime, percentage = percentage, trackTitle = TrackTitle, trackArtist = TrackArtist });
                }
                return Json(new { currentTime = currentTime, totalTime = totalTime, percentage = percentage });

            }
            return Json(new { currentTime = 0, totalTime = 0, percentage = 0 });
        }

        // start playlist
        [HttpPost("playlist/{playlistId}")]
        public ActionResult PlayPlaylist(int PlaylistId)
        {
            playlistFiles.Clear();
            currentPlaylist.Clear();
            currentSongIndex = 0;
            var playlists = _context.Playlists.Where(p => p.PlaylistId == PlaylistId).SelectMany(m => m.MusicList).ToList();

            foreach (var item in playlists)
            {
                playlistFiles.Add(item.TrackFile.ToString());
                currentPlaylist.Add(item);
            }

            // Initialize playback
            PlayNextSong();

            // Return JSON so client can parse reliably
            return Ok(new { message = "Playlist started", trackCount = playlistFiles.Count });
        }


        private void PlayNextSong()
        {
            if (currentSongIndex < playlistFiles.Count)
            {
                // Dispose previous output/audio if present
                output?.Dispose();
                audioFile?.Dispose();

                string filePath = playlistFiles[currentSongIndex];

                using (var reader = new Mp3FileReader(filePath))
                {
                    sampleRate = reader.Mp3WaveFormat.SampleRate;
                    Console.WriteLine("Sample Rate: " + sampleRate);
                }

                audioFile = new AudioFileReader(filePath);

                output = new WaveOutEvent(); // Or DirectSoundOut
                output.Init(audioFile);

                // For playlist playback we want this handler to advance to next track when natural end occurs.
                output.PlaybackStopped -= OnPlaybackPlaylistStopped;
                output.PlaybackStopped += OnPlaybackPlaylistStopped;

                output.Play();

                // IMPORTANT: mark audio as playing so Stop/Pause endpoints work
                isAudioPlaying = true;

                Console.WriteLine($"Playing playlist track #{currentSongIndex}: {filePath}");
            }
            else
            {
                // playlist finished — ensure state cleared
                isAudioPlaying = false;
                playlistFiles.Clear();
                currentPlaylist.Clear();
                currentSongIndex = 0;
                Console.WriteLine("Playlist finished.");
            }
        }

        private void OnPlaybackPlaylistStopped(object sender, StoppedEventArgs e)
        {
            currentSongIndex++;
            PlayNextSong();
        }


    } 
}
