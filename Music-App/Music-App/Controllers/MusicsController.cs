/*
 * Import the models and the required packages to perform MVC and database actions as well as NAudio for audio actions
 */
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

namespace Music_App.Controllers // use the controller namespace
{
    public class MusicsController : Controller // Create the musics controller
    {
        /*
         * create all the variables needed to perform the NAudio operations and our custom playlist operations
         */
        private static WaveOutEvent output; // audio output device (i.e. speakers)
        private static AudioFileReader audioFile; // file used to stream from
        private static int sampleRate; // song sample rate used to calculate the fast-forward and rewind values
        private static TimeSpan duration; // song duration
        private static bool isAudioPlaying = false; // additional variable to confirm if audio is playing
        private long maxFileSize = 1024 * 1024 * 3; // makes the max a file can be to 3mb 
        private static List<string> playlistFiles = new List<string>();
        private static List<Music> currentPlaylist = new List<Music>();
        private static int currentSongIndex = 0; // stores the current song index in the playlist
        /*
         * dependency injection variables
         */
        private readonly IConfiguration _config;
        private readonly MusicContext _context;
        /*
         * inject the config and database context into the controller
         */
        public MusicsController(MusicContext context, IConfiguration config)
        {
            _context = context;
            _config = config;
        }

        /// <summary>
        /// Index GET route which shows the user all the songs in the database as well as the controls for audio playback
        /// </summary>
        /// <returns>returns the view with all the songs in the database as long as the database is not empty</returns>
        public async Task<IActionResult> Index()
        {
            return _context.Musics != null
                ? View(await _context.Musics.ToListAsync())
                : Problem("Entity set 'MusicContext.Musics'  is null.");
        }

        /// <summary>
        /// Database GET route that shows all the songs in the database as well as the search bar to filter the results
        /// </summary>
        /// <param name="searchQuery"></param>
        /// <returns>returns the view with either all the songs or the filtered list of songs</returns>
        public async Task<IActionResult> Database(string searchQuery)
        {
            var musics = _context.Musics.AsQueryable(); // get all the songs
            /*
             * filter the songs by title or artist if the search query is not empty
             */
            if (!string.IsNullOrEmpty(searchQuery))
            {
                musics = musics.Where(m => m.TrackTitle.Contains(searchQuery) || m.TrackArtist.Contains(searchQuery));
            }
            ViewData["SearchQuery"] = searchQuery; // store the search query to ViewData
            return View(await musics.ToListAsync()); // return the songs to the view
        }
        
        /// <summary>
        /// Create GET route that shows the user the form to add a song to the database
        /// </summary>
        /// <returns>returns the Create view</returns>
        public IActionResult Create()
        {
            return View();
        }

        /// <summary>
        /// Create POST route that saves the song to the database if it passes all the validations
        /// </summary>
        /// <param name="music"></param>
        /// <returns>returns the user to the database view if song data is valid otherwise returns the view with error messages</returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("TrackId,TrackFile,TrackTitle,TrackArtist,TrackLength")] Music music)
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
            return View(music); // return error messages to the view and keep the entered data
        }

        /// <summary>
        /// Edit GET route that allows the user to edit the information of the selected song
        /// </summary>
        /// <param name="id"></param>
        /// <returns>
        /// returns not found if the song is not in the database or is invalid otherwise returns the view
        /// with the song data to the user
        /// </returns>
        public async Task<IActionResult> Edit(int? id)
        {
            /*
             * return not found if the song is not in the database or the database is empty
             */
            if (id == null || _context.Musics == null)
            {
                return NotFound();
            }
            var music = await _context.Musics.FindAsync(id); // find the selected song
            /*
             * return not found if the song has no data
             */
            if (music == null)
            {
                return NotFound();
            }
            return View(music); // return the song data to the view
        }

        /// <summary>
        /// Edit POST route that tries to save the edited song data into the database
        /// </summary>
        /// <param name="id"></param>
        /// <param name="music"></param>
        /// <returns>redirects the user to the database view if song data is valid otherwise return one of the various errors</returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("TrackId,TrackFile,TrackTitle,TrackArtist,TrackLength")] Music music)
        {
            /*
             * return not found if the id is not in the database
             */
            if (id != music.TrackId)
            {
                return NotFound();
            }
            /*
             * try to save the song to the database
             */
            if (ModelState.IsValid)
            {
                try
                {
                    // Get the base path for file storage from configuration or use default
                    var basePath = _config.GetValue<string>("FileStorage") ?? Path.Combine(Directory.GetCurrentDirectory(), "FileStorage");

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
                /*
                 * return not found if the song already exists in the database with that id otherwise throw the exception to console
                 */
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
                return RedirectToAction(nameof(Database)); // send user back to the database view
            }
            return View(music); // return the error messages to the view with the entered data
        }

        /// <summary>
        /// Delete GET route that gets the id of the selected song
        /// </summary>
        /// <param name="id"></param>
        /// <returns>returns not found if the song is not found otherwise shows the delete song alert to the user</returns>
        public async Task<IActionResult> Delete(int? id)
        {
            /*
             * return not found if the song does not exist in the database or the database is empty
             */
            if (id == null || _context.Musics == null)
            {
                return NotFound();
            }
            var music = await _context.Musics.FirstOrDefaultAsync(m => m.TrackId == id); // find the first match
            /*
             * return not found if the song is not found or has no data
             */
            if (music == null)
            {
                return NotFound();
            }
            return View(music); // return the delete message to the user
        }

        /// <summary>
        /// DeleteConfirmed POST route that confirms the deletion of the selected song from the database via a javascript alert
        /// </summary>
        /// <param name="id"></param>
        /// <returns>returns problem if the database is empty toherwise redirects user to the Database view</returns>
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            /*
             * return problem if the database is empty
             */
            if (_context.Musics == null)
            {
                return Problem("Entity set 'MusicContext.Musics'  is null.");
            }
            var music = await _context.Musics.FindAsync(id); // find the selected song
            /*
             * remove the song from the database as long as the song exists
             */
            if (music != null)
            {
                _context.Musics.Remove(music);
            }
            await _context.SaveChangesAsync(); // save the changes to the database
            return RedirectToAction(nameof(Database)); // send user back to the database view
        }

        /// <summary>
        /// auto-generated function that checks if a song exists in the database
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        private bool MusicExists(int id)
        {
            return (_context.Musics?.Any(e => e.TrackId == id)).GetValueOrDefault();
        }

        /// <summary>
        /// play/{trackId} POST route that sets up the output and audioFile objects to be ready to play audio then starts
        /// to play the audio, it also gets the sample rate of the song and sets the default volume to 50%
        /// </summary>
        /// <param name="trackId"></param>
        /// <returns>returns not found if the song or file is not found otherwise returns Ok</returns>
        [HttpPost("play/{trackId}")]
        public IActionResult Play(int trackId)
        {
            /*
             * if there is no output device initialized already, create a new one
             */
            if (output == null)
            {
                output = new WaveOutEvent();
                output.PlaybackStopped += OnPlaybackStopped;
                Console.WriteLine("Initialized output object.");
            }
            /*
             * Dispose of the previous file if there was one already
             */
            if (audioFile != null)
            {
                audioFile.Dispose();
            }
            var track = _context.Musics.FirstOrDefault(m => m.TrackId == trackId); // find the song
            if (track == null) return NotFound("Track not found.");

            var audioPath = track.TrackFile?.Trim().Trim('"'); // get the filepath from the track data
            if (string.IsNullOrWhiteSpace(audioPath) || !System.IO.File.Exists(audioPath))
            {
                return NotFound("Audio file path is missing or file not found.");
            }

            audioFile = new AudioFileReader(audioPath); // open the song
            /*
             * get the sample rate from the file for use in calculating the fast-forward and rewind amounts
             */
            using (var reader = new Mp3FileReader(audioPath))
            {
                sampleRate = reader.Mp3WaveFormat.SampleRate;
                Console.WriteLine("Sample Rate: " + sampleRate);
            }
            output.Init(audioFile); // tell the output device which file to use as a sound source
            output.Play(); // play the audio
            isAudioPlaying = true;
            audioFile.Volume = 0.5f; // set volume to 50% when play is clicked
            Console.WriteLine("Audio started playing.");
            return Ok(new { message = "Audio is playing", trackTitle = track.TrackTitle, trackArtist = track.TrackArtist });
        }

        /// <summary>
        /// Stop POST route that stops all streams which triggers OnPlaybackPlaylistStopped()
        /// </summary>
        /// <returns>returns badrequest if no audio is playing otherwise returns Ok</returns>
        [HttpPost("stop")]
        public IActionResult Stop()
        {
            Console.WriteLine("Stop button pressed.");
            /*
             * check if there is audio playing or a valid output instance
             */
            if (output == null || !isAudioPlaying)
            {
                Console.WriteLine("No audio is currently playing.");
                return BadRequest("No audio is currently playing.");
            }
            output.Stop(); // stop output
            audioFile = null; // get rid of the file stream
            /*
             * clear the playlist progress data
             */
            playlistFiles.Clear();
            currentPlaylist.Clear();
            currentSongIndex = 0;
            isAudioPlaying = false;
            Console.WriteLine("Audio stopped successfully.");
            return Ok("Audio stopped");
        }

        /// <summary>
        /// Pause POST route that pauses/starts the audio when the pause button is pressed
        /// </summary>
        /// <returns>returns bad request if no output is found otherwise returns Ok</returns>
        [HttpPost("pause")]
        public IActionResult Pause()
        {
            Console.WriteLine("Puase button pressed.");
            /*
             * return badrequest if the output is not initialized
             */
            if (output == null)
            {
                Console.WriteLine("No audio is currently playing.");
                return BadRequest("No audio is currently playing.");
            }
            /*
             * if no audio is playing then start playing the audio
             */
            if (!isAudioPlaying)
            {
                output.Play();
                isAudioPlaying = true;
                Console.WriteLine("Audio is playing.");
                return Ok("Audio resumed");
            }
            /*
             * if audio is playing then pause the audio
             */
            else
            {
                output.Pause();
                isAudioPlaying = false;
                Console.WriteLine("Audio paused successfully.");
                return Ok("Audio paused");
            }
        }

        /// <summary>
        /// Rewind POST route that rewinds the audio by changing the position in the file when the rewind button is pressed
        /// </summary>
        /// <returns>returns bad request if no audio is playing otherwise returns Ok</returns>
        [HttpPost("rewind")]
        public IActionResult Rewind()
        {
            Console.WriteLine("Rewind button pressed.");
            /*
             * check if there is audio playing or a valid output instance
             */
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

        /// <summary>
        /// Forward POST route that fast-forwards the audio by changing the position in the file when the fast-forward button is pressed
        /// </summary>
        /// <returns>returns bad request if no audio is playing otherwise returns Ok</returns>
        [HttpPost("forward")]
        public IActionResult Forward()
        {
            Console.WriteLine("Forward button pressed.");
            /*
             * check if there is audio playing or a valid output instance
             */
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
        /// Up POST route that raises the volume of the audioFileReader (open audio file) when the Vol up button is pressed
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
        /// Down POST route that decreases the volume of the audioFileReader (open audio file) when the Vol down button is pressed
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
        
        /// <summary>
        /// function that frees resources if the stop button is clicked
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        private void OnPlaybackStopped(object sender, StoppedEventArgs args)
        {
            output?.Dispose(); // get rid of output object
            audioFile?.Dispose(); // get rid of audio file
            output = null;
            audioFile = null;
            isAudioPlaying = false;
            Console.WriteLine("Playback stopped, resources disposed.");
        }
        
        /// <summary>
        /// function that trims extra spaces from the filepaths
        /// </summary>
        /// <param name="path"></param>
        /// <returns>returns the trimmed file path</returns>
        private static string TrimPath(string path)
        {
            if (string.IsNullOrWhiteSpace(path)) return path;
            path = path.Trim();
            if (path.Length >= 2 && path.StartsWith("\"") && path.EndsWith("\""))
            {
                path = path.Substring(1, path.Length - 2);
            }
            return path.Trim();
        }

        /// <summary>
        /// GetPlaybackProgress GET route that is queried to get the status of the song playback progress
        /// </summary>
        /// <returns>returns the current progress of the track playback or 0</returns>
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

        /// <summary>
        /// PlayPlaylist POST route that starts playing the first song in a selected playlist
        /// </summary>
        /// <param name="PlaylistId"></param>
        /// <returns>returns the number of songs in the playlist</returns>
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

        /// <summary>
        /// function that handles the switching to the next song in the playlist once the current song is finished
        /// </summary>
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

        /// <summary>
        /// function that checks if the current song is stopped AND part of a playlist, if so then increment the index
        /// and play the next song in the playlist
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnPlaybackPlaylistStopped(object sender, StoppedEventArgs e)
        {
            currentSongIndex++;
            PlayNextSong();
        }
    } 
}