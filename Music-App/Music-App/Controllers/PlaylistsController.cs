using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Music_App.Migrations;
using Music_App.Models;

namespace Music_App.Controllers
{
    public class PlaylistsController : Controller
    {
        private readonly MusicContext _context;

        public PlaylistsController(MusicContext context)
        {
            _context = context;
        }

        // GET: Playlists
        public IActionResult Index()
        {
            var playlist = _context.Playlists.FirstOrDefault();
            if (playlist == null)
            {
                return View(new Playlist());
            }
            ViewBag.InPlaylist = _context.Playlists.Where(p => p.PlaylistId == playlist.PlaylistId).SelectMany(m => m.MusicList).ToList();
            return View(playlist);
        }

        [HttpPost]
        public async Task<IActionResult> Index(int? PlaylistId)
        {
            if (PlaylistId == null)
            {
                return NotFound();
            }
            var playlist = await _context.Playlists.FindAsync(PlaylistId);
            ViewBag.InPlaylist = _context.Playlists.Where(p => p.PlaylistId == PlaylistId).SelectMany(m => m.MusicList).ToList();
            if(ViewBag.InPlaylist == null)
            {
                return NotFound();
            }
            return View(playlist);
        }

        // GET: Playlists
        public async Task<IActionResult> Database()
        {
            return _context.Musics != null ?
                View(await _context.Playlists.ToListAsync()) :
                Problem("Entity set 'MusicContext.Musics'  is null.");
        }


        // GET: Playlists/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Playlists/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("PlaylistId,PlaylistName")] Playlist playlist)
        {
            if (ModelState.IsValid)
            {
                _context.Add(playlist);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(playlist);
        }

        // GET: Playlists/Edit/5
        public async Task<IActionResult> EditName(int? id)
        {
            if (id == null || _context.Playlists == null)
            {
                return NotFound();
            }

            var playlist = await _context.Playlists.FindAsync(id);
            if (playlist == null)
            {
                return NotFound();
            }
            return View(playlist);
        }

        // POST: Playlists/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        public async Task<IActionResult> EditName(int id, [Bind("PlaylistId,PlaylistName")] Playlist playlist)
        {
            if (id != playlist.PlaylistId)
            {
                return NotFound();
            }
            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(playlist);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!PlaylistExists(playlist.PlaylistId))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            return View(playlist);
        }


        // GET: Playlists/Edit/5
        public async Task<IActionResult> EditMusic(int? id)
        {
            if (id == null || _context.Playlists == null)
            {
                return NotFound();
            }

            var playlist = await _context.Playlists.FindAsync(id);
            if (playlist == null)
            {
                return NotFound();
            }
            ViewBag.Musics = _context.Musics.ToList();
            ViewBag.InPlaylist = _context.Playlists.Where(p => p.PlaylistId == playlist.PlaylistId).SelectMany(m => m.MusicList).ToList();
            return View(playlist);
        }

        [HttpPost]
        public async Task<IActionResult> EditMusic(int? musicId, int? playlistId)
        {

            var playlist = await _context.Playlists.FindAsync(playlistId);
            var song = await _context.Musics.FindAsync(musicId);

            if (song == null || playlist == null)
            {
                return NotFound();
            }
            
            playlist.MusicList.Add(song);
            await _context.SaveChangesAsync();

            ViewBag.Musics = _context.Musics.ToList();
            ViewBag.InPlaylist = _context.Playlists.Where(p => p.PlaylistId == playlist.PlaylistId).SelectMany(m => m.MusicList).ToList();
            return View(playlist);
        }


        // Not working...
        [HttpPost]
        public async Task<IActionResult> DeleteMusic(int? musicId, int? playlistId)
        {
            var playlist = await _context.Playlists.FindAsync(playlistId);
            var song = await _context.Musics.FindAsync(musicId);

            if (song == null || playlist == null)
            {
                return NotFound();
            }

            var currentPlaylist = _context.Playlists.Where(p => p.PlaylistId == playlist.PlaylistId).SelectMany(p => p.MusicList).ToList();
            currentPlaylist.Remove(song);
            
            await _context.SaveChangesAsync();

            ViewBag.Musics = _context.Musics.ToList();
            ViewBag.InPlaylist = _context.Playlists.Where(p => p.PlaylistId == playlist.PlaylistId).SelectMany(p => p.MusicList).ToList();
            return RedirectToAction("Index");
        }



        // GET: Playlists/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null || _context.Playlists == null)
            {
                return NotFound();
            }

            var playlist = await _context.Playlists
                .FirstOrDefaultAsync(m => m.PlaylistId == id);
            if (playlist == null)
            {
                return NotFound();
            }

            return View(playlist);
        }

        // POST: Playlists/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            if (_context.Playlists == null)
            {
                return Problem("Entity set 'MusicContext.Playlist'  is null.");
            }
            var playlist = await _context.Playlists.FindAsync(id);
            if (playlist != null)
            {
                _context.Playlists.Remove(playlist);
            }
            
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool PlaylistExists(int id)
        {
          return (_context.Playlists?.Any(e => e.PlaylistId == id)).GetValueOrDefault();
        }
    }
}
