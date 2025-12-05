using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Music_App.Models;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using Microsoft.Extensions.Configuration;


namespace Music_App.Controllers
{
    public class MusicsController : Controller
    {
        private static WaveOutEvent output;
        private static AudioFileReader audioFile;
        private readonly MusicContext _context;
        private static bool isAudioPlaying = false;
        // makes the max a file can be to 3mb 
        private long maxFileSize = 1024 * 1024 * 3; //  = 3 MB
        private readonly IConfiguration _config;
        
        

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

        // GET: Musics
        public async Task<IActionResult> Database()
        {
            return _context.Musics != null
                ? View(await _context.Musics.ToListAsync())
                : Problem("Entity set 'MusicContext.Musics'  is null.");
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
                    music.TrackFile = TrimPath(music.TrackFile);
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
        
        [HttpPost("play")]
        public IActionResult Play()
        {
            if (output == null)
            {
                output = new WaveOutEvent();
                output.PlaybackStopped += OnPlaybackStopped;
                Console.WriteLine("Initialized output object.");
            }

            if (audioFile == null)
            {
                // hardcoded file path for testing purposes
                //audioFile = new AudioFileReader(@"C:\Users\Nicoh\Downloads\Future-Technology(chosic.com).mp3")
                //audioFile = new AudioFileReader(@"C:\Users\scemb\Desktop\Music Files\Piano Sonata No. 14 in C-Sharp Minor, Op. 27 No. 2 Moonlight I. Adagio sostenuto.mp3");

                var track = _context.Musics.FirstOrDefault();
                if (track == null) return NotFound("No track found in the database.");

                var audioPath = track.TrackFile?.Trim().Trim('"');
                if (string.IsNullOrWhiteSpace(audioPath) || !System.IO.File.Exists(audioPath))
                    return NotFound("Audio file path is missing or file not found.");

                audioFile = new AudioFileReader(audioPath);
                output.Init(audioFile);
                Console.WriteLine("Initialized audioFile.");
            }

            if (output.PlaybackState != PlaybackState.Playing)
            {
                output.Play();
                isAudioPlaying = true;
                Console.WriteLine("Audio started playing.");
            }
            else
            {
                Console.WriteLine("Audio is already playing.");
            }
            return Ok("Audio is playing");
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
            isAudioPlaying = false;
            Console.WriteLine("Audio stopped successfully.");
            return Ok("Audio stopped");
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
    }
}
