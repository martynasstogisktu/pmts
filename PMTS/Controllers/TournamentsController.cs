using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using PMTS.Authentication;
using PMTS.Models;

namespace PMTS.Controllers
{
    public class TournamentsController : Controller
    {
        private readonly PSQLcontext _context;
        private readonly PmtsJwt _pmtsJwt;
        //private readonly UsersController _usersController;

        public TournamentsController(PSQLcontext context, PmtsJwt pmtsJwt)
        {
            _context = context;
            _pmtsJwt = pmtsJwt;
            //_usersController = usersController;
        }

        // GET: Tournaments
        public async Task<IActionResult> Index()
        {
            return _context.Tournament != null ?
                        View(await _context.Tournament.Where(t => !t.IsPrivate).ToListAsync()) :
                        Problem("Entity set 'PSQLcontext.Tournament'  is null.");
        }

        // GET: Tournaments/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            
            if (id == null || _context.Tournament == null)
            {
                return NotFound();
            }

            var tournament = await _context.Tournament
                .FirstOrDefaultAsync(m => m.Id == id);
            
            if (tournament == null)
            {
                return NotFound();
            }
            try
            {
                if (Request.Cookies["userCookie"] != null)
                {
                    string cookie = Request.Cookies["userCookie"];
                    JwtSecurityToken validatedToken = _pmtsJwt.Validate(cookie);
                    User user = GetUser(int.Parse(validatedToken.Issuer));
                    if (tournament.Organizer == user.Name)
                    {
                        TempData["Organizer"] = "True";
                    }

                }
            }
            catch (Exception ex)
            {
                //prisijungimas neprivalomas, tad įvykus autentifikacijos klaidai nieko nedaroma
            }
            if(tournament.RestrictedTypes)
            {
                TempData["RestrictedTypes"] = "True";
            }
            return View(tournament);
        }

        // GET: Tournaments/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Tournaments/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,Name,IsPrivate,RestrictedTypes,Active,StartTime,EndTime")] Tournament tournament)
        {
            try
            {
                string cookie = Request.Cookies["userCookie"];
                JwtSecurityToken validatedToken = _pmtsJwt.Validate(cookie);
                User user = GetUser(int.Parse(validatedToken.Issuer));
                tournament.Organizer = user.Name;
                if (!ModelState.IsValid)
                {
                    foreach (var modelState in ViewData.ModelState.Values)
                    {
                        foreach (ModelError error in modelState.Errors)
                        {
                            Console.WriteLine(error.ErrorMessage);
                        }
                    }
                }
                if (ModelState.IsValid)
                {
                    tournament.StartTime = tournament.StartTime.ToUniversalTime();
                    tournament.EndTime = tournament.EndTime.ToUniversalTime();
                    if (user.Tournaments == null)
                    {
                        user.Tournaments = new List<Tournament>();
                    }
                    user.Tournaments.Add(tournament);
                    _context.Update(user);
                    await _context.SaveChangesAsync();
                    return RedirectToAction(nameof(Index));
                }
                return View(tournament);
            }
            catch (Exception ex)
            {
                TempData["AuthStatus"] = "AuthError";
                return RedirectToAction("Login");
            }
            
        }

        // GET: Tournaments/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null || _context.Tournament == null)
            {
                return NotFound();
            }

            var tournament = await _context.Tournament.FindAsync(id);
            if (tournament == null)
            {
                return NotFound();
            }
            return View(tournament);
        }

        // POST: Tournaments/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Name,IsPrivate,RestrictedTypes,Active,StartTime,EndTime")] Tournament tournament)
        {
            if (id != tournament.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(tournament);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!TournamentExists(tournament.Id))
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
            return View(tournament);
        }

        // GET: Tournaments/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null || _context.Tournament == null)
            {
                return NotFound();
            }

            var tournament = await _context.Tournament
                .FirstOrDefaultAsync(m => m.Id == id);
            if (tournament == null)
            {
                return NotFound();
            }

            return View(tournament);
        }

        // POST: Tournaments/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            if (_context.Tournament == null)
            {
                return Problem("Entity set 'PSQLcontext.Tournament'  is null.");
            }
            var tournament = await _context.Tournament.FindAsync(id);
            if (tournament != null)
            {
                _context.Tournament.Remove(tournament);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool TournamentExists(int id)
        {
            return (_context.Tournament?.Any(e => e.Id == id)).GetValueOrDefault();
        }

        public User GetUser(int id)
        {
            return _context.Users.FirstOrDefault(e => e.Id == id);
        }
    }
}
