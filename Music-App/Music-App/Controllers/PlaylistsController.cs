/*
 * Import the models and the required packages to perform MVC and database actions
 */
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

namespace Music_App.Controllers // use the controller namespace
{
    public class PlaylistsController : Controller // Create the playlist controller
    {
        /*
         * Create and inject the database context
         */
        private readonly MusicContext _context;

        public PlaylistsController(MusicContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Index GET route which shows the songs in the current playlist to the user
        /// </summary>
        /// <returns>returns the first playlist in the database or an empty playlist</returns>
        public IActionResult Index()
        {
            /*
            * return the first non-empty playlist or a new empty playlist
            */
            var playlist = _context.Playlists.FirstOrDefault();
            if (playlist == null)
            {
                return View(new Playlist());
            }
            ViewBag.InPlaylist = _context.Playlists.Where(p => p.PlaylistId == playlist.PlaylistId).SelectMany(m => m.MusicList).ToList(); // get all the songs in the playlist
            return View(playlist); // return the playlist data to the view
        }

        /// <summary>
        /// Index POST route which shows the songs in the specified playlist to the user
        /// </summary>
        /// <param name="PlaylistId"></param>
        /// <returns>returns not found if the playlist is not in the database otherwise returns the playlist entity</returns>
        [HttpPost]
        public async Task<IActionResult> Index(int? PlaylistId)
        {
            /*
             * return not found if the user selects a playlist no longer in the database
             */
            if (PlaylistId == null)
            {
                return NotFound();
            }
            /*
             * find the selected playlist in the database
             */
            var playlist = await _context.Playlists.FindAsync(PlaylistId);
            ViewBag.InPlaylist = _context.Playlists.Where(p => p.PlaylistId == PlaylistId).SelectMany(m => m.MusicList).ToList();
            /*
             * return not found if the playlist is empty
             */
            if(ViewBag.InPlaylist == null)
            {
                return NotFound();
            }
            return View(playlist); // return the playlist to the view
        }

        /// <summary>
        /// Database GET route which shows all the playlists in the database to the user
        /// </summary>
        /// <returns>returns the database or an error if the Musics table is empty</returns>
        public async Task<IActionResult> Database()
        {
            /*
             * return the list of playlists in the database as long as it is not empty
             */
            return _context.Musics != null ?
                View(await _context.Playlists.ToListAsync()) :
                Problem("Entity set 'MusicContext.Musics'  is null.");
        }


        /// <summary>
        /// Create GET route which lets the user enter a name for a new playlist entity
        /// </summary>
        /// <returns>returns the create view</returns>
        public IActionResult Create()
        {
            return View();
        }

        /// <summary>
        /// Create POST route that will create and save the playlist entity in the database if valid
        /// </summary>
        /// <param name="playlist"></param>
        /// <returns>returns the view with errors visible or redirects the user to the playlist index view</returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("PlaylistId,PlaylistName")] Playlist playlist)
        {
            /*
             * only save the playlist model to the database if it passes all of the validations
             * then return the user to the index view after creation
             */
            if (ModelState.IsValid)
            {
                _context.Add(playlist);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(playlist); // return the playlist to the view if the data is invalid with error messages
        }

        /// <summary>
        /// EditName GET route that lets the user edit the name of the selected playlist if found
        /// </summary>
        /// <param name="id"></param>
        /// <returns>returns the view as long as the playlist is not empty and the playlist is found in the database</returns>
        public async Task<IActionResult> EditName(int? id)
        {
            /*
             * return not found if the playlist is not in the database or the database is not created
             */
            if (id == null || _context.Playlists == null)
            {
                return NotFound();
            }
            var playlist = await _context.Playlists.FindAsync(id); // find the selected playlist in the database
            /*
             * return not found if the playlist is empty
             */
            if (playlist == null)
            {
                return NotFound();
            }
            return View(playlist); // return the playlist to the view
        }

        /// <summary>
        /// EditName POST route that will save the new playlist name of the selected playlist to the database if valid
        /// </summary>
        /// <param name="id"></param>
        /// <param name="playlist"></param>
        /// <returns>
        /// redirects the user to the playlist index view if name is valid otherwise will show the view again
        /// with any errors
        /// </returns>
        [HttpPost]
        public async Task<IActionResult> EditName(int id, [Bind("PlaylistId,PlaylistName")] Playlist playlist)
        {
            /*
             * return not found if the playlist is not found
             */
            if (id != playlist.PlaylistId)
            {
                return NotFound();
            }
            /*
             * if the playlist is valid, try to update the info in the database
             */
            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(playlist);
                    await _context.SaveChangesAsync();
                }
                /*
                 * check if the playlist already exists and if not return a not found
                 */
                catch (DbUpdateConcurrencyException)
                {
                    if (!PlaylistExists(playlist.PlaylistId))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw; // throw the exception to the console if none of the above happens
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            return View(playlist); // return the playlist to the view
        }


        /// <summary>
        /// EditMusic GET route that lets the user change the songs inside a playlist
        /// </summary>
        /// <param name="id"></param>
        /// <returns>
        /// returns not found if the playlist is not found in the database or the database is empty, otherwise
        /// shows all the songs in the playlist and the available songs in the Musics database
        /// </returns>
        public async Task<IActionResult> EditMusic(int? id)
        {
            /*
             * return not found if the database is empty or the id is not found
             */
            if (id == null || _context.Playlists == null)
            {
                return NotFound();
            }
            var playlist = await _context.Playlists.FindAsync(id); // find the selected playlist in the database
            /*
             * return not found if found playlist does not exist
             */
            if (playlist == null)
            {
                return NotFound();
            }
            ViewBag.Musics = _context.Musics.ToList(); // add the available songs from Musics table to the ViewBag
            ViewBag.InPlaylist = _context.Playlists.Where(p => p.PlaylistId == playlist.PlaylistId).SelectMany(m => m.MusicList).ToList(); // add all the songs in the playlist to the ViewBag
            return View(playlist); // return playlist to view
        }

        /// <summary>
        /// EditMusic POST route that save the newly added songs to the selected playlist in the database
        /// </summary>
        /// <param name="musicId"></param>
        /// <param name="playlistId"></param>
        /// <returns>
        /// return not found if the selected song or playlist does not exist in either database otherwise
        /// return the updated playlist to the view
        /// </returns>
        [HttpPost]
        public async Task<IActionResult> EditMusic(int? musicId, int? playlistId)
        {
            var playlist = await _context.Playlists.FindAsync(playlistId); // find the playlist in the database
            var song = await _context.Musics.FindAsync(musicId); // find the song in Musics database
            /*
             * return not found if either the song or playlist don't exist in either database
             */
            if (song == null || playlist == null)
            {
                return NotFound();
            }
            playlist.MusicList.Add(song); // add the song to playlist
            await _context.SaveChangesAsync(); // save changes to the playlist database
            ViewBag.Musics = _context.Musics.ToList(); // add the available songs to the ViewBag
            ViewBag.InPlaylist = _context.Playlists.Where(p => p.PlaylistId == playlist.PlaylistId).SelectMany(m => m.MusicList).ToList(); // add the songs in the playlist to the ViewBag
            return View(playlist); // return the updated playlist to the view
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



        /// <summary>
        /// Delete GET route which deletes a playlist from the database
        /// </summary>
        /// <param name="id"></param>
        /// <returns>
        /// returns not found if the playlist is not found in the database or the database is empty, otherwise
        /// return the playlist to the view
        /// </returns>
        public async Task<IActionResult> Delete(int? id)
        {
            /*
             * return not found if the database is empty or the id is not found
             */
            if (id == null || _context.Playlists == null)
            {
                return NotFound();
            }
            var playlist = await _context.Playlists.FirstOrDefaultAsync(m => m.PlaylistId == id); // get the first match
            /*
             * return not found if found playlist does not exist
             */
            if (playlist == null)
            {
                return NotFound();
            }
            return View(playlist); // return the playlist to the view
        }

        /// <summary>
        /// DeleteConfirmed POST route that will delete a playlist from the database
        /// </summary>
        /// <param name="id"></param>
        /// <returns>redirects the user to the playlist index view</returns>
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            /*
             * return a problem if the playlist database is empty
             */
            if (_context.Playlists == null)
            {
                return Problem("Entity set 'MusicContext.Playlist'  is null.");
            }
            var playlist = await _context.Playlists.FindAsync(id); // find the selected playlist in the database
            /*
             * remove the playlist from the database as long as it exists
             */
            if (playlist != null)
            {
                _context.Playlists.Remove(playlist);
            }
            /*
             * save the changes made and return the user the playlist index view
             */
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        /// <summary>
        /// auto-generated function to check if a playlist exists in the database or not
        /// </summary>
        /// <param name="id"></param>
        /// <returns>returns any playlist</returns>
        private bool PlaylistExists(int id)
        {
          return (_context.Playlists?.Any(e => e.PlaylistId == id)).GetValueOrDefault();
        }
    }
}