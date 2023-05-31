using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.IdentityModel.Tokens;
using PMTS.Authentication;
using PMTS.DTOs;
using PMTS.Models;
using static System.Net.Mime.MediaTypeNames;
using static System.Runtime.InteropServices.JavaScript.JSType;
using Azure.Identity;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Blobs.Specialized;
using Microsoft.Azure.CognitiveServices.Vision.ComputerVision;
using Microsoft.Azure.CognitiveServices.Vision.ComputerVision.Models;
//using PMTS.Migrations;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using System.Reflection.Metadata;

namespace PMTS.Controllers
{
    public class TournamentsController : Controller
    {
        private readonly PSQLcontext _context;
        private readonly PmtsJwt _pmtsJwt;
        private readonly BlobContainerClient _blobContainerClient;
        //private readonly UsersController _usersController;

        public TournamentsController(PSQLcontext context, PmtsJwt pmtsJwt)
        {
            _context = context;
            _pmtsJwt = pmtsJwt;
            _blobContainerClient = new BlobServiceClient(Environment.GetEnvironmentVariable("Storage")).GetBlobContainerClient("pmts-pic");
            //_usersController = usersController;
        }

        // GET: Tournaments
        public async Task<IActionResult> Index()
        {
            return _context.Tournament != null ?
                        View(await _context.Tournament.Where(t => !t.IsPrivate).ToListAsync()) :
                        Problem("Entity set 'PSQLcontext.Tournament'  is null.");
        }

        public async Task<IActionResult> IndexAdmin()
        {
            try
            {
                string cookie = Request.Cookies["userCookie"];
                JwtSecurityToken validatedToken = _pmtsJwt.Validate(cookie);
                User user = GetUser(int.Parse(validatedToken.Issuer));
                if (user.Admin)
                {
                    return _context.Tournament != null ?
                        View(await _context.Tournament.ToListAsync()) :
                        Problem("Entity set 'PSQLcontext.Tournament'  is null.");
                }
                else
                {
                    return RedirectToAction("Index", "Home");
                }

            }
            catch (Exception ex)
            {
                TempData["AuthStatus"] = "AuthError";
                return RedirectToAction("Index", "Home");
            }
        }

        // GET: MyTournaments
        public async Task<IActionResult> MyTournaments()
        {
            try
            {
                string cookie = Request.Cookies["userCookie"];
                JwtSecurityToken validatedToken = _pmtsJwt.Validate(cookie);
                User user = GetUser(int.Parse(validatedToken.Issuer));

                MyTournamentsDTO myTournamentsDTO = new MyTournamentsDTO();

                List<Tournament> organizedTournaments = await _context.Tournament.Where(t => t.UserId == user.Id).ToListAsync();
                myTournamentsDTO.organizedTournaments = organizedTournaments;

                List<Contestant> contestants = await _context.Contestant.Where(u => u.UserId == user.Id).ToListAsync();
                foreach (Contestant contestant in contestants)
                    myTournamentsDTO.memberOfTournaments.Add(await _context.Tournament.FirstOrDefaultAsync(t => t.Id == contestant.TournamentId));

                return View(myTournamentsDTO);
            }
            catch (Exception ex)
            {
                TempData["AuthStatus"] = "AuthError";
                return RedirectToAction("Index", "Home");
            }
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
            //jei turnyras prasidejo
            if (tournament.Active && tournament.StartTime.ToUniversalTime() < DateTime.Now.ToUniversalTime())
            {
                tournament.Ongoing = true;
            }
            else
                tournament.Ongoing = false;
            //jei turnyras baigesi
            if (tournament.EndTime.ToUniversalTime() < DateTime.Now.ToUniversalTime())
            {
                tournament.Active = false;
                tournament.Ongoing = false;
            }

            //Active = true, Ongoing = false - galima prisijungti bet dar negalima kelti nuotrauku
            //Ongoing = true - turnyre galima dalyvauti ir kelti nuotraukas
            //Active = false - turnyre negalima dalyvauti (bagesi arba buvo nutrauktas)

            if(tournament.Active)
            {
                TempData["Active"] = "True";
            }
            if(tournament.Ongoing)
            {
                TempData["Ongoing"] = "True";
            }

            TempData["OrganizerName"] = tournament.Organizer;

            _context.Tournament.Include(tournament => tournament.Contestants).ToList();
            
            try
            {
                if (Request.Cookies["userCookie"] != null)
                {
                    string cookie = Request.Cookies["userCookie"];
                    JwtSecurityToken validatedToken = _pmtsJwt.Validate(cookie);
                    User user = GetUser(int.Parse(validatedToken.Issuer));
                    
                    TempData["LoggedIn"] = "True";
                    if (tournament.UserId == user.Id)
                    {
                        TempData["Organizer"] = "True";
                    }
                    else
                    {
                        TempData["Organizer"] = "False";
                    }
                    if (user.Admin)
                    {
                        TempData["IsAdmin"] = "True";
                    }
                    if (!_context.Contestant.IsNullOrEmpty())
                    {
                        var contestant = _context.Contestant.FirstOrDefault(m => m.UserId == user.Id && m.TournamentId == tournament.Id);
                        if (contestant != null)
                        {
                            TempData["IsContestant"] = "True";
                            if (contestant.Removed)
                                TempData["Removed"] = "True";
                        }
                        else
                        {
                            TempData["IsContestant"] = "False";
                        }
                    }
                    else
                    {
                        TempData["IsContestant"] = "False";
                    }
                    if (tournament.IsPrivate)
                    {
                        //jei privatus turnyras, tikrinama ar naudotojas yra dalyvis, rengejas
                        if (!((_context.Contestant.FirstOrDefault(m => m.UserId == user.Id && m.TournamentId == tournament.Id) != null) || tournament.UserId == user.Id || user.Admin))
                        {
                            return RedirectToAction("Index", "Home");
                        }
                    }
                }
                else
                {
                    TempData["LoggedIn"] = "False";
                    if (tournament.IsPrivate)
                        return RedirectToAction("Index", "Home");
                }
            }
            catch (Exception ex)
            {
                //prisijungimas neprivalomas, tad įvykus autentifikacijos nenukreipiama i prisijungima (tik jei privatus)
                TempData["LoggedIn"] = "False";
                if(tournament.IsPrivate)
                    return RedirectToAction("Index", "Home");
            }
            return View(tournament);
        }

        // GET: Tournaments/Create
        public IActionResult Create()
        {
            try
            {
                string cookie = Request.Cookies["userCookie"];
                JwtSecurityToken validatedToken = _pmtsJwt.Validate(cookie);
                User user = GetUser(int.Parse(validatedToken.Issuer));
                if(user == null)
                {
                    TempData["AuthStatus"] = "AuthError";
                    return RedirectToAction("Index", "Home");
                }
                if (user.Admin)
                {
                    return RedirectToAction("Index", "Home");
                }
                return View();
            }
            catch (Exception ex)
            {
                TempData["AuthStatus"] = "AuthError";
                return RedirectToAction("Index", "Home");
            }
        }

        // GET: Tournaments/ContestantPhotos
        public async Task<IActionResult> ContestantPhotos(int? id)
        {
            if (id == null || _context.Contestant == null)
            {
                return NotFound();
            }

            var contestant = await _context.Contestant
                .FirstOrDefaultAsync(m => m.Id == id);

            if (contestant == null)
            {
                return NotFound();
            }

            var tournament = await _context.Tournament
                .FirstOrDefaultAsync(m => m.Id == contestant.TournamentId);
            if (tournament == null)
            {
                return NotFound();
            }

            _context.Contestant.Include(contestant => contestant.Photos).ToList();
                            
            try
            {
                if (Request.Cookies["userCookie"] != null)
                {
                    string cookie = Request.Cookies["userCookie"];
                    JwtSecurityToken validatedToken = _pmtsJwt.Validate(cookie);
                    User user = GetUser(int.Parse(validatedToken.Issuer));
                    if (user.Admin || tournament.UserId == user.Id || contestant.UserId == user.Id)
                    {
                        TempData["CanDelete"] = "True";
                    }
                    
                    //jei privatus turnyras, perziureti galima tik prisijungus ir jame dalyvaujant arba esant turnyro rengeju ar administratoriumi
                    if ((_context.Contestant.FirstOrDefault(m => m.UserId == user.Id && m.TournamentId == tournament.Id) != null) || tournament.UserId == user.Id || user.Admin)
                    {
                        return View(contestant);
                    }
                    if (!tournament.IsPrivate)
                    {
                        return View(contestant);
                    }
                    else
                    {
                        return RedirectToAction("Index", "Home");
                    }
                }
                else
                    return RedirectToAction("Index", "Home");
            }
            catch (Exception ex)
            {
                TempData["AuthStatus"] = "AuthError";
                return RedirectToAction("Index", "Home");
            }
            

        }

        // POST: Tournaments/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Name,IsPrivate,Active,StartTime,EndTime,DefaultPoints")] Tournament tournament)
        {
            try
            {
                string cookie = Request.Cookies["userCookie"];
                JwtSecurityToken validatedToken = _pmtsJwt.Validate(cookie);
                User user = GetUser(int.Parse(validatedToken.Issuer));
                if (user == null)
                {
                    TempData["AuthStatus"] = "AuthError";
                    return RedirectToAction("Index", "Home");
                }
                if (user.Admin)
                {
                    return RedirectToAction("Index", "Home");
                }
                tournament.Organizer = user.Name;

                if (ModelState.IsValid)
                {
                    tournament.StartTime = tournament.StartTime.ToUniversalTime();
                    tournament.EndTime = tournament.EndTime.ToUniversalTime();
                    tournament.RestrictedTypes = false;
                    if (user.Tournaments == null)
                    {
                        user.Tournaments = new List<Tournament>();
                    }
                    user.Tournaments.Add(tournament);
                    _context.Update(user);
                    await _context.SaveChangesAsync();
                    return RedirectToAction("Details", new { Id = tournament.Id });
                }
                return View(tournament);
            }
            catch (Exception ex)
            {
                TempData["AuthStatus"] = "AuthError";
                return RedirectToAction("Index", "Home");
            }
            
        }

        // POST: Tournaments/Join/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [ValidateAntiForgeryToken]
        [HttpPost, ActionName("Join")]
        public async Task<IActionResult> Join(int? id)
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
            try
            {
                string cookie = Request.Cookies["userCookie"];
                JwtSecurityToken validatedToken = _pmtsJwt.Validate(cookie);
                User user = GetUser(int.Parse(validatedToken.Issuer));
                if (user == null)
                {
                    TempData["AuthStatus"] = "AuthError";
                    return RedirectToAction("Index", "Home");
                }
                if (user.Admin)
                {
                    return RedirectToAction("Index", "Home");
                }
                if (tournament.Organizer == user.Name)
                {
                    TempData["TournamentError"] = "OwnerJoin";
                    return RedirectToAction("Details", new { Id = id });
                }
                else
                {
                    TempData["TournamentError"] = "";
                    if (tournament.Contestants == null)
                    {
                        tournament.Contestants = new List<Contestant>();
                    }
                    Contestant contestant = new Contestant();
                    contestant.UserName = user.Name;
                    contestant.TournamentName = tournament.Name;
                    contestant.UserId = user.Id;
                    contestant.TournamentId = tournament.Id;

                    tournament.Contestants.Add(contestant);
                    _context.Update(tournament);
                    await _context.SaveChangesAsync();
                    TempData["JoinStatus"] = "JoinSuccess";
                    return RedirectToAction("Details", new { Id = id });
                }
            }
            catch (Exception ex)
            {
                TempData["AuthStatus"] = "AuthError";
                return RedirectToAction("Index", "Home");
            }

        }

        // GET: Tournaments/AddUserToTournament/5
        public async Task<IActionResult> AddUserToTournament(int? id)
        {
            if (id == null || _context.Tournament == null || _context.Users == null)
            {
                return NotFound();
            }

            var tournament = await _context.Tournament.FindAsync(id);
            if (tournament == null)
            {
                return NotFound();
            }
            try
            {
                string cookie = Request.Cookies["userCookie"];
                JwtSecurityToken validatedToken = _pmtsJwt.Validate(cookie);
                User user = GetUser(int.Parse(validatedToken.Issuer));
                if (user == null)
                {
                    TempData["AuthStatus"] = "AuthError";
                    return RedirectToAction("Index", "Home");
                }
                if (user.Admin)
                {
                    return RedirectToAction("Index", "Home");
                }
                if (tournament.UserId != user.Id)
                {
                    return RedirectToAction("Details", new { Id = id });
                }
                else
                {
                    return View();
                }
            }
            catch (Exception ex)
            {
                TempData["AuthStatus"] = "AuthError";
                return RedirectToAction("Index", "Home");
            }

        }

        // POST: Tournaments/AddUserToTournament/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddUserToTournament(int? id, AddUserDTO addUserDTO)
        {
            if (id == null || _context.Tournament == null || _context.Users == null)
            {
                return NotFound();
            }

            var tournament = await _context.Tournament.FindAsync(id);
            var newContestant = _context.Users.FirstOrDefault(e => e.Name == addUserDTO.Name);
            if (tournament == null)
            {
                return NotFound();
            }
            if (newContestant == null)
            {
                TempData["AddStatus"] = "AddFailed";
                return View();
            }
            try
            {
                string cookie = Request.Cookies["userCookie"];
                JwtSecurityToken validatedToken = _pmtsJwt.Validate(cookie);
                User user = GetUser(int.Parse(validatedToken.Issuer));
                if (user == null)
                {
                    TempData["AuthStatus"] = "AuthError";
                    return RedirectToAction("Index", "Home");
                }
                if (user.Admin)
                {
                    return RedirectToAction("Index", "Home");
                }
                if (tournament.UserId != user.Id)
                {
                    return RedirectToAction("Details", new { Id = id });
                }
                else
                {
                    var checkContestant = _context.Contestant.FirstOrDefault(e => e.UserId == newContestant.Id && e.TournamentId == tournament.Id);
                    if(checkContestant != null)
                    {
                        TempData["AddStatus"] = "AlreadyMember";
                        return View();
                    }
                    if (tournament.Contestants == null)
                    {
                        tournament.Contestants = new List<Contestant>();
                    }
                    Contestant contestant = new Contestant();
                    contestant.UserName = newContestant.Name;
                    contestant.TournamentName = tournament.Name;
                    contestant.UserId = newContestant.Id;
                    contestant.TournamentId = newContestant.Id;

                    tournament.Contestants.Add(contestant);
                    _context.Update(tournament);
                    await _context.SaveChangesAsync();
                    TempData["AddStatus"] = "AddSuccess";
                    return View();
                }
            }
            catch (Exception ex)
            {
                TempData["AuthStatus"] = "AuthError";
                return RedirectToAction("Index", "Home");
            }
        }

        // Tournaments/RemoveContestantFromTournament/5
        //[HttpPost, ActionName("RemoveContestantFromTournament")]
        //[ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveContestantFromTournament(int id)
        {
            if (_context.Contestant == null)
            {
                return Problem("Entity set 'PSQLcontext.Contestant'  is null.");
            }
            var contestant = await _context.Contestant.FindAsync(id);
            var tournament = await _context.Tournament.Where(t => t.Id == contestant.TournamentId).FirstOrDefaultAsync();

            if (contestant != null)
            {
                try
                {
                    string cookie = Request.Cookies["userCookie"];
                    JwtSecurityToken validatedToken = _pmtsJwt.Validate(cookie);
                    User currentUser = GetUser(int.Parse(validatedToken.Issuer));
                    if (currentUser == null)
                    {
                        TempData["AuthStatus"] = "AuthError";
                        return RedirectToAction("Index", "Home");
                    }
                    if (!currentUser.Admin && tournament.UserId != currentUser.Id)
                    {
                        return RedirectToAction("Index", "Home");
                    }
                    else
                    {
                        //delete user photos to save space
                        List<Photo> photos = _context.Photo.Where(p => p.ContestantId == contestant.Id).ToList();
                        foreach (Photo photo in photos)
                        {
                            BlobClient blobClient = _blobContainerClient.GetBlobClient(photo.Name);
                            blobClient.Delete();
                            BlobClient blobClientThumb = _blobContainerClient.GetBlobClient(photo.ThumbName);
                            blobClientThumb.Delete();
                            _context.Photo.Remove(photo);
                        }

                        contestant.Removed = true;
                        contestant.Points = 0;
                        _context.Contestant.Update(contestant);
                    }
                }
                catch (Exception ex)
                {
                    TempData["AuthStatus"] = "AuthError";
                    return RedirectToAction("Index", "Home");
                }
            }

            await _context.SaveChangesAsync();
            return RedirectToAction("Details", new { Id = contestant.TournamentId });
        }

        // Tournaments/LeaveTournament
        //[HttpPost, ActionName("LeaveTournament")]
        //[ValidateAntiForgeryToken]
        public async Task<IActionResult> LeaveTournament()
        {
            if (_context.Contestant == null)
            {
                return Problem("Entity set 'PSQLcontext.Contestant'  is null.");
            }

            try
            {
                string cookie = Request.Cookies["userCookie"];
                JwtSecurityToken validatedToken = _pmtsJwt.Validate(cookie);
                User currentUser = GetUser(int.Parse(validatedToken.Issuer));
                if (currentUser == null)
                {
                    TempData["AuthStatus"] = "AuthError";
                    return RedirectToAction("Index", "Home");
                }

                Contestant contestant = await _context.Contestant.Where(t => t.UserId == currentUser.Id).FirstOrDefaultAsync();
                Tournament tournament = await _context.Tournament.Where(t => t.Id == contestant.TournamentId).FirstOrDefaultAsync();
                
                //delete user photos to save space
                List<Photo> photos = _context.Photo.Where(p => p.ContestantId == contestant.Id).ToList();
                foreach (Photo photo in photos)
                {
                    BlobClient blobClient = _blobContainerClient.GetBlobClient(photo.Name);
                    blobClient.Delete();
                    BlobClient blobClientThumb = _blobContainerClient.GetBlobClient(photo.ThumbName);
                    blobClientThumb.Delete();
                    _context.Photo.Remove(photo);
                }

                contestant.Removed = true;
                contestant.Points = 0;
                _context.Contestant.Update(contestant);

                await _context.SaveChangesAsync();
                return RedirectToAction("Details", new { Id = contestant.TournamentId });

            }
            catch (Exception ex)
            {
                TempData["AuthStatus"] = "AuthError";
                return RedirectToAction("Index", "Home");
            }
        }

        // GET: Tournaments/CheckPhoto/5
        public async Task<IActionResult> CheckPhoto(int? id)
        {
            if (id == null || _context.Tournament == null || _context.Users == null)
            {
                return NotFound();
            }

            var tournament = await _context.Tournament.FindAsync(id);
            if (tournament == null)
            {
                return NotFound();
            }
            try
            {
                string cookie = Request.Cookies["userCookie"];
                JwtSecurityToken validatedToken = _pmtsJwt.Validate(cookie);
                User user = GetUser(int.Parse(validatedToken.Issuer));
                if (user == null)
                {
                    TempData["AuthStatus"] = "AuthError";
                    return RedirectToAction("Index", "Home");
                }
                if (user.Admin)
                {
                    return RedirectToAction("Index", "Home");
                }
                if (tournament.UserId != user.Id)
                {
                    return RedirectToAction("Details", new { Id = id });
                }
                else
                {
                    List<Photo> photos = _context.Photo.Where(p => p.TournamentId == tournament.Id && !p.BirdDetected).ToList();

                    return View(photos);
                }
            }
            catch (Exception ex)
            {
                TempData["AuthStatus"] = "AuthError";
                return RedirectToAction("Index", "Home");
            }

        }

        // POST: Tournaments/ConfirmPhoto/5
        // nuotraukos patvirtinimas kad yra paukstis (rankiniu budu)
        [ValidateAntiForgeryToken]
        [HttpPost, ActionName("ConfirmPhoto")]
        public async Task<IActionResult> ConfirmPhoto(int? id)
        {
            if (_context.Tournament == null || _context.Users == null || id == null)
            {
                return NotFound();
            }

            
            var photo = _context.Photo.FirstOrDefault(e => e.Id == id);
            var tournament = await _context.Tournament.FindAsync(photo.TournamentId);
            if (tournament == null)
            {
                return NotFound();
            }
            if (photo == null)
            {
                TempData["ConfirmStatus"] = "ConfirmFailed";
                return RedirectToAction("CheckPhoto", new { Id = tournament.Id });
            }
            try
            {
                string cookie = Request.Cookies["userCookie"];
                JwtSecurityToken validatedToken = _pmtsJwt.Validate(cookie);
                User user = GetUser(int.Parse(validatedToken.Issuer));
                if (user == null)
                {
                    TempData["AuthStatus"] = "AuthError";
                    return RedirectToAction("Index", "Home");
                }
                if (user.Admin)
                {
                    return RedirectToAction("Index", "Home");
                }
                if (tournament.UserId != user.Id)
                {
                    return RedirectToAction("Details", new { Id = tournament.Id });
                }
                else
                {
                    try
                    {
                        Contestant contestant = _context.Contestant.FirstOrDefault(e => e.Id == photo.ContestantId);
                        contestant.Photos.First(p => p.Id == photo.Id).BirdDetected = true;
                        contestant.Points += photo.Points;
                        _context.Contestant.Update(contestant);
                        await _context.SaveChangesAsync();
                        TempData["ConfirmStatus"] = "ConfirmSuccess";
                        return RedirectToAction("CheckPhoto", new { Id = tournament.Id });
                    }
                    catch
                    {
                        TempData["ConfirmStatus"] = "ConfirmFailed";
                        return RedirectToAction("CheckPhoto", new { Id = tournament.Id });
                    }
                }
            }
            catch (Exception ex)
            {
                TempData["AuthStatus"] = "AuthError";
                return RedirectToAction("Index", "Home");
            }
        }

        // Tournaments/DeleteContestantPhoto/5
        //[ValidateAntiForgeryToken]
        //[HttpPost, ActionName("DeleteContestantPhoto")]
        public async Task<IActionResult> DeleteContestantPhoto(int? id)
        {
            if (_context.Tournament == null || _context.Users == null || id == null)
            {
                return NotFound();
            }


            var photo = _context.Photo.FirstOrDefault(e => e.Id == id);
            var tournament = await _context.Tournament.FindAsync(photo.TournamentId);
            if (tournament == null)
            {
                return NotFound();
            }
            if (photo == null)
            {
                TempData["ConfirmStatus"] = "ConfirmFailed";
                return RedirectToAction("CheckPhoto", new { Id = tournament.Id });
            }
            try
            {
                string cookie = Request.Cookies["userCookie"];
                JwtSecurityToken validatedToken = _pmtsJwt.Validate(cookie);
                User user = GetUser(int.Parse(validatedToken.Issuer));

                Contestant contestant = _context.Contestant.FirstOrDefault(e => e.Id == photo.ContestantId);
                if (user == null)
                {
                    TempData["AuthStatus"] = "AuthError";
                    return RedirectToAction("Index", "Home");
                }
                if (tournament.UserId != user.Id && !user.Admin && user.Id != contestant.UserId)
                {
                    return RedirectToAction("Index", "Home");
                }
                else
                {
                    BlobClient blobClient = _blobContainerClient.GetBlobClient(photo.Name);
                    blobClient.Delete();
                    BlobClient blobClientThumb = _blobContainerClient.GetBlobClient(photo.ThumbName);
                    blobClientThumb.Delete();
                    contestant.Points -= photo.Points;
                    _context.Photo.Remove(photo);
                    await _context.SaveChangesAsync();
                    return RedirectToAction("Details", new { Id = photo.TournamentId });

                }
            }
            catch (Exception ex)
            {
                TempData["AuthStatus"] = "AuthError";
                return RedirectToAction("Index", "Home");
            }
        }

        // GET: Tournaments/AddPhoto/5
        public async Task<IActionResult> AddPhoto(int? id)
        {
            //System.Diagnostics.Debug.WriteLine("------------------------");
            if (id == null || _context.Tournament == null)
            {
                return NotFound();
            }

            var tournament = await _context.Tournament.FindAsync(id);
            if (tournament == null)
            {
                return NotFound();
            }

            try
            {
                string cookie = Request.Cookies["userCookie"];
                JwtSecurityToken validatedToken = _pmtsJwt.Validate(cookie);
                User user = GetUser(int.Parse(validatedToken.Issuer));
                if (user == null)
                {
                    TempData["AuthStatus"] = "AuthError";
                    return RedirectToAction("Index", "Home");
                }
                if (user.Admin)
                {
                    return RedirectToAction("Index", "Home");
                }

                if (_context.Contestant.FirstOrDefault(m => m.UserName == user.Name && m.TournamentName == tournament.Name) != null)
                {
                    //jei naudotojas yra turnyro dalyvis
                    PhotoDTO photoDTO = new PhotoDTO();
                    photoDTO.TournamentId = (int)id;
                    return View(photoDTO);
                }
                else
                {
                    //jei ne dalyvis, grazina i turnyro aprasyma
                    //joks pranesimas nenurodomas, nes ikelimo puslapis neturi buti pasiekiamams naudotojams nedalyvaujantiems turnyre
                    return RedirectToAction("Details", new { Id = id });
                }
            }
            catch (Exception ex)
            {
                TempData["AuthStatus"] = "AuthError";
                return RedirectToAction("Index", "Home");
            }

        }

        // POST: Tournaments/AddPhoto/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddPhoto(int? id, PhotoDTO photoDTO)
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

            try
            {
                string cookie = Request.Cookies["userCookie"];
                JwtSecurityToken validatedToken = _pmtsJwt.Validate(cookie);
                User user = GetUser(int.Parse(validatedToken.Issuer));
                if (user == null)
                {
                    TempData["AuthStatus"] = "AuthError";
                    return RedirectToAction("Index", "Home");
                }
                if (user.Admin)
                {
                    return RedirectToAction("Index", "Home");
                }
                Contestant? contestant = _context.Contestant.FirstOrDefault(m => m.UserName == user.Name && m.TournamentName == tournament.Name);

                if (contestant != null)
                {
                    string[] permittedExtensions = { ".jpg", ".jpeg", ".png", ".bmp" };
                    var ext = Path.GetExtension(photoDTO.PhotoData.FileName);
                    if (string.IsNullOrEmpty(ext) || !permittedExtensions.Contains(ext))
                    {
                        ModelState.AddModelError("PhotoData", "Failo tipas netinkamas.");
                        return View(id);
                    }
                    if (photoDTO.BirdsN <= 0)
                    {
                        ModelState.AddModelError("BirdsN", "Paukščių skaičius turi būti teigiamas skaičius.");
                        return View(id);
                    }

                //jei naudotojas yra turnyro dalyvis
                using (var memoryStream = new MemoryStream())
                    {
                        await photoDTO.PhotoData.CopyToAsync(memoryStream);

                        // failai nedidesni nei 4 MB
                        if (memoryStream.Length < 4194304)
                        {
                            BinaryData binaryData = new BinaryData(memoryStream.ToArray());
                            if (contestant.Photos == null)
                            {
                                contestant.Photos = new List<Photo>();
                            }
                            _context.Update(tournament);
                            Photo photo = new Photo();
                            photo.TournamentId = tournament.Id;
                            photo.ContestantId = contestant.Id;
                            photo.BirdN = photoDTO.BirdsN;
                            photo.Points = tournament.DefaultPoints * photo.BirdN;
                            contestant.Photos.Add(photo);

                            await _context.SaveChangesAsync();

                            string fileName = photo.Id.ToString() + ext;
                            string thumbFileName = photo.Id.ToString() + "_thumb" + ext;

                            await UploadBlob(photo.Id, fileName, thumbFileName, binaryData);

                            TempData["PhotoAdded"] = "True";
                            return RedirectToAction("Details", new { Id = id });
                        }
                        else
                        {
                            TempData["Error"] = "true";
                            return View();
                        }
                    }
                }
                else
                {
                    //jei ne dalyvis, grazina i turnyro aprasyma
                    //joks pranesimas nenurodomas, nes ikelimo puslapis neturi buti pasiekiamams naudotojams nedalyvaujantiems turnyre
                    return RedirectToAction("Details", new { Id = id });
                }
            }
            catch (Exception ex)
            {
                TempData["AuthStatus"] = "AuthError";
                return RedirectToAction("Index", "Home");
            }

        }

        private async Task UploadBlob(int id, string name, string thumbName, BinaryData binaryData)
        {
            var photo = await _context.Photo.FindAsync(id);
            BlobClient blobClient = _blobContainerClient.GetBlobClient(name);
            await blobClient.UploadAsync(binaryData, true);
            photo.Name = name;
            photo.ThumbName = thumbName;

            DetectResult detectResult = await AnalyzeImage(name, thumbName);
            int detectBirds = 0; //number of birds found in photo
            foreach (var obj in detectResult.Objects)
            {
                if (obj.ObjectProperty.Equals("bird"))
                {
                    detectBirds++;
                    photo.BirdDetected = true;
                }
            }
                        
            if (detectBirds == photo.BirdN)
            {
                var contestant = await _context.Contestant.FindAsync(photo.ContestantId);
                contestant.Points += photo.Points;
                _context.Contestant.Update(contestant);
            }
            else
            {
                //jei nesutampa nurodytu pauksciu skaicius su surastu, nuotrauka pazymima patikrai
                photo.BirdDetected = false;
            }
            _context.Photo.Update(photo);
            await _context.SaveChangesAsync();
        }

        private async Task<DetectResult> AnalyzeImage(string name, string thumbName)
        {
            ComputerVisionClient client =
              new ComputerVisionClient(new ApiKeyServiceClientCredentials(Environment.GetEnvironmentVariable("VisionKey")))
              { Endpoint = Environment.GetEnvironmentVariable("VisionEndpoint") };

            DetectResult detectResult = await client.DetectObjectsAsync("https://pmts.blob.core.windows.net/pmts-pic/" + name);

            Stream stream = await client.GenerateThumbnailAsync(300, 300, "https://pmts.blob.core.windows.net/pmts-pic/" + name, true);

            BlobClient blobClient = _blobContainerClient.GetBlobClient(thumbName);
            await blobClient.UploadAsync(stream, true);


            return detectResult;
        }

        // GET: Tournaments/Photo/5
        public async Task<IActionResult> Photo(int? id)
        {

            if (id == null || _context.Photo == null)
            {
                return NotFound();
            }

            var photo = await _context.Photo
                .FirstOrDefaultAsync(m => m.Id == id);

            if (photo == null)
            {
                return NotFound();
            }

            var tournament = await _context.Tournament
                .FirstOrDefaultAsync(m => m.Id == photo.TournamentId);
            if (tournament == null)
            {
                return NotFound();
            }

            _context.Contestant.Include(contestant => contestant.Photos).ToList();

            if (!tournament.IsPrivate)
            {
                return View(photo);
            }

            else
            {
                //jei privatus turnyras, perziureti galima tik prisijungus ir jame dalyvaujant
                try
                {
                    if (Request.Cookies["userCookie"] != null)
                    {
                        string cookie = Request.Cookies["userCookie"];
                        JwtSecurityToken validatedToken = _pmtsJwt.Validate(cookie);
                        User user = GetUser(int.Parse(validatedToken.Issuer));

                        if ((_context.Contestant.FirstOrDefault(m => m.UserId == user.Id && m.TournamentId == tournament.Id) != null) || tournament.UserId == user.Id || user.Admin)
                        {
                            return View(photo);
                        }
                        else
                        {
                            return RedirectToAction("Index", "Home");
                        }
                    }
                    else
                        return RedirectToAction("Index", "Home");
                }
                catch (Exception ex)
                {
                    TempData["AuthStatus"] = "AuthError";
                    return RedirectToAction("Index", "Home");
                }
            }
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

            try
            {
                string cookie = Request.Cookies["userCookie"];
                JwtSecurityToken validatedToken = _pmtsJwt.Validate(cookie);
                User user = GetUser(int.Parse(validatedToken.Issuer));
                if (user == null)
                {
                    TempData["AuthStatus"] = "AuthError";
                    return RedirectToAction("Index", "Home");
                }
                if (!user.Admin && tournament.UserId != user.Id)
                {
                    return RedirectToAction("Index", "Home");
                }
                else
                {
                    return View(tournament);
                }
            }
            catch (Exception ex)
            {
                TempData["AuthStatus"] = "AuthError";
                return RedirectToAction("Index", "Home");
            }

            
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
                try
                {
                    string cookie = Request.Cookies["userCookie"];
                    JwtSecurityToken validatedToken = _pmtsJwt.Validate(cookie);
                    User user = GetUser(int.Parse(validatedToken.Issuer));
                    if (user == null)
                    {
                        TempData["AuthStatus"] = "AuthError";
                        return RedirectToAction("Index", "Home");
                    }
                    if (!user.Admin && tournament.UserId != user.Id)
                    {
                        return RedirectToAction("Index", "Home");
                    }
                    else
                    {
                        List<Photo> photos = _context.Photo.Where(p => p.TournamentId == tournament.Id).ToList();
                        foreach (Photo photo in photos)
                        {
                            BlobClient blobClient = _blobContainerClient.GetBlobClient(photo.Name);
                            blobClient.Delete();
                            BlobClient blobClientThumb = _blobContainerClient.GetBlobClient(photo.ThumbName);
                            blobClientThumb.Delete();
                        }
                        _context.Tournament.Remove(tournament);
                    }
                }
                catch (Exception ex)
                {
                    TempData["AuthStatus"] = "AuthError";
                    return RedirectToAction("Index", "Home");
                }
                
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
