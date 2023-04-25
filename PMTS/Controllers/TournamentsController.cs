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
using PMTS.Migrations;
using Microsoft.EntityFrameworkCore.Metadata.Internal;

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
            if (tournament.EndTime.ToUniversalTime() > DateTime.Now.ToUniversalTime())
            {
                tournament.Active = false;
                tournament.Ongoing = false;
            }
            //Ongoing = true - turnyre galima dalyvauti
            //Ongoing = false - turnyre negalima dalyvauti (bagesi arba buvo nutrauktas)
            

            try
            {
                if (Request.Cookies["userCookie"] != null)
                {
                    string cookie = Request.Cookies["userCookie"];
                    JwtSecurityToken validatedToken = _pmtsJwt.Validate(cookie);
                    User user = GetUser(int.Parse(validatedToken.Issuer));
                    TempData["LoggedIn"] = "True";
                    if (tournament.Organizer == user.Name)
                    {
                        TempData["Organizer"] = "True";
                    }
                    else
                    {
                        TempData["Organizer"] = "False";
                    }
                    if (!_context.Contestant.IsNullOrEmpty())
                    {
                        if(_context.Contestant.FirstOrDefault(m => m.UserId == user.Id && m.TournamentId == tournament.Id) != null)
                        {
                            TempData["IsContestant"] = "True";
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
                }
                else
                {
                    TempData["LoggedIn"] = "False";
                }
            }
            catch (Exception ex)
            {
                //prisijungimas neprivalomas, tad įvykus autentifikacijos nenukreipiama i prisijungima
                TempData["LoggedIn"] = "False";
            }
            if(tournament.RestrictedTypes)
            {
                TempData["RestrictedTypes"] = "True";
            }
            else
            {
                TempData["RestrictedTypes"] = "False";
            }
            return View(tournament);
        }

        // GET: Tournaments/Create
        public IActionResult Create()
        {
            return View();
        }

        // GET: Tournaments/ContestantPhotosTest
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

            if(!tournament.IsPrivate)
            {
                return View(contestant);
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

                        if (_context.Contestant.FirstOrDefault(m => m.UserId == user.Id && m.TournamentId == tournament.Id) != null)
                        {
                            return View(contestant);
                        }
                        else
                        {
                            return RedirectToAction("Details", new { Id = tournament.Id });
                        }
                    }
                    else
                        return RedirectToAction("Details", new { Id = tournament.Id });
                }
                catch (Exception ex)
                {
                    TempData["AuthStatus"] = "AuthError";
                    return RedirectToAction("Login", "Users");
                }
            }

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
                //if (!ModelState.IsValid)
                //{
                //    foreach (var modelState in ViewData.ModelState.Values)
                //    {
                //        foreach (ModelError error in modelState.Errors)
                //        {
                //            Console.WriteLine(error.ErrorMessage);
                //        }
                //    }
                //}
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
                    return RedirectToAction("Details", new { Id = tournament.Id });
                }
                return View(tournament);
            }
            catch (Exception ex)
            {
                TempData["AuthStatus"] = "AuthError";
                return RedirectToAction("Login", "Users");
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
                return RedirectToAction("Login", "Users");
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
                return RedirectToAction("Login", "Users");
            }


            //test code\/
            //if (id == null || _context.Tournament == null)
            //{
            //    return NotFound();
            //}

            //var tournament = await _context.Tournament.FindAsync(id);
            //if (tournament == null)
            //{
            //    return NotFound();
            //}

            //PhotoDTO photoDTO = new PhotoDTO();
            //photoDTO.TournamentId = (int)id;
            //return View(photoDTO);
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

            //try
            //{
                string cookie = Request.Cookies["userCookie"];
                JwtSecurityToken validatedToken = _pmtsJwt.Validate(cookie);
                User user = GetUser(int.Parse(validatedToken.Issuer));
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
                            //using var stream = System.IO.File.Create("A:/img/photo" + ext);
                            //stream.Write(memoryStream.ToArray(), 0, memoryStream.ToArray().Length);
                            BinaryData binaryData = new BinaryData(memoryStream.ToArray());
                            //var file = new AppFile()
                            //{
                            //    Content = memoryStream.ToArray()
                            //};

                            //_dbContext.File.Add(file);
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

                            await UploadBlob(photo.Id, fileName, thumbFileName, binaryData); //nelaukia kol ikelimas baigiamas

                            TempData["PhotoAdded"] = "True";
                            return RedirectToAction("Details", new { Id = id });
                        }
                        else
                        {
                            ModelState.AddModelError("PhotoData", "Failas per didelis.");
                            return View(id);
                        }
                    }
                }
                else
                {
                    //jei ne dalyvis, grazina i turnyro aprasyma
                    //joks pranesimas nenurodomas, nes ikelimo puslapis neturi buti pasiekiamams naudotojams nedalyvaujantiems turnyre
                    return RedirectToAction("Details", new { Id = id });
                }
            //}
            //catch (Exception ex)
            //{
            //    TempData["AuthStatus"] = "AuthError";
            //    return RedirectToAction("Login", "Users");
            //}

            //test code
            //string cookie = Request.Cookies["userCookie"];
            //JwtSecurityToken validatedToken = _pmtsJwt.Validate(cookie);
            //User user = GetUser(int.Parse(validatedToken.Issuer));

            //if (_context.Contestant.FirstOrDefault(m => m.UserName == user.Name && m.TournamentName == tournament.Name) != null)
            //{
            //    string[] permittedExtensions = { ".jpg", ".jpeg", ".png", ".bmp" };
            //    var ext = Path.GetExtension(photoDTO.PhotoData.FileName);
            //    if (string.IsNullOrEmpty(ext) || !permittedExtensions.Contains(ext))
            //    {
            //        ModelState.AddModelError("PhotoData", "Failo tipas netinkamas.");
            //        return View(id);
            //    }

            //    //jei naudotojas yra turnyro dalyvis
            //    using (var memoryStream = new MemoryStream())
            //    {
            //        await photoDTO.PhotoData.CopyToAsync(memoryStream);

            //        // failai nedidesni nei 4 MB
            //        if (memoryStream.Length < 4194304)
            //        {
            //            using var stream = System.IO.File.Create("A:/img/photo" + ext);
            //            stream.Write(memoryStream.ToArray(), 0, memoryStream.ToArray().Length);
            //            //var file = new AppFile()
            //            //{
            //            //    Content = memoryStream.ToArray()
            //            //};

            //            //_dbContext.File.Add(file);

            //            //await _dbContext.SaveChangesAsync();
            //            TempData["PhotoAdded"] = "True";
            //            return RedirectToAction("Details", new { Id = id });
            //        }
            //        else
            //        {
            //            ModelState.AddModelError("PhotoData", "Failo tipas netinkamas.");
            //            return View(id);
            //        }
            //    }
            //}
            //else
            //{
            //    //jei ne dalyvis, grazina i turnyro aprasyma
            //    //joks pranesimas nenurodomas, nes ikelimo puslapis neturi buti pasiekiamams naudotojams nedalyvaujantiems turnyre
            //    return RedirectToAction("Details", new { Id = id });
            //}
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

            return View(photo);
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
            try
            {
                string cookie = Request.Cookies["userCookie"];
                JwtSecurityToken validatedToken = _pmtsJwt.Validate(cookie);
                User user = GetUser(int.Parse(validatedToken.Issuer));
                if (tournament.Organizer == user.Name)
                {
                    return View(tournament);
                }
                else
                {
                    return RedirectToAction("Index");
                }
            }
            catch (Exception ex)
            {
                TempData["AuthStatus"] = "AuthError";
                return RedirectToAction("Login", "Users");
            }
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
                    string cookie = Request.Cookies["userCookie"];
                    JwtSecurityToken validatedToken = _pmtsJwt.Validate(cookie);
                    User user = GetUser(int.Parse(validatedToken.Issuer));
                    if (tournament.Organizer == user.Name)
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
                    else
                    {
                        return RedirectToAction("Index");
                    }
                }
                catch (Exception ex)
                {
                    TempData["AuthStatus"] = "AuthError";
                    return RedirectToAction("Login", "Users");
                }
                
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
