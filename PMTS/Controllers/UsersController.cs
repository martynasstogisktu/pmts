using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security;
using System.Threading.Tasks;
using Azure.Storage.Blobs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using NuGet.Protocol.Plugins;
using PMTS.Authentication;
using PMTS.DTOs;
using PMTS.Models;

namespace PMTS.Controllers
{
    public class UsersController : Controller
    {
        private readonly PSQLcontext _context;
        private readonly PmtsJwt _pmtsJwt;
        private readonly BlobContainerClient _blobContainerClient;

        public UsersController(PSQLcontext context, PmtsJwt pmtsJwt)
        {
            _context = context;
            _pmtsJwt = pmtsJwt;
            _blobContainerClient = new BlobServiceClient(Environment.GetEnvironmentVariable("Storage")).GetBlobContainerClient("pmts-pic");
        }

        // GET: Users
        public async Task<IActionResult> Index()
        {
            try
            {
                string cookie = Request.Cookies["userCookie"];
                JwtSecurityToken validatedToken = _pmtsJwt.Validate(cookie);
                User user = GetUser(int.Parse(validatedToken.Issuer));
                if (user.Admin)
                {
                    return _context.Users != null ?
                          View(await _context.Users.ToListAsync()) :
                          Problem("Entity set 'PSQLcontext.Users'  is null.");
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

        // GET: Users/Details
        public async Task<IActionResult> Details()
        {
            try
            {
                string cookie = Request.Cookies["userCookie"];
                JwtSecurityToken validatedToken = _pmtsJwt.Validate(cookie);
                User user = GetUser(int.Parse(validatedToken.Issuer));
                if (user.Admin)
                {
                    //mygtukas "Mano turnyrai" neatvaizduojamas administratoriams
                    TempData["IsAdmin"] = "true";
                    TempData["RegisterEnabled"] = Environment.GetEnvironmentVariable("RegisterEnabled");
                }
                else
                    TempData["IsAdmin"] = "false";
                return View(user);
            }
            catch (Exception ex)
            {
                TempData["AuthStatus"] = "AuthError";
                return RedirectToAction("Index", "Home");
            }
        }


        [Route("Register")]
        public IActionResult Register()
        {
            TempData["RegisterEnabled"] = "false";
            TempData["RegisterEnabled"] = Environment.GetEnvironmentVariable("RegisterEnabled");
            return View();
        }

        // POST: Register
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [Route("Register")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register([Bind("Name,Email,Password")] User user)
        {
            if(Environment.GetEnvironmentVariable("RegisterEnabled") == "true")
            {
                //visada grazina kad el. pastas arba vardas uzimtas (o ne kuris is ju), kad atskleistu maziau info
                if (GetUser(user.Name) != null || GetUserByEmail(user.Email) != null)
                {
                    ModelState.AddModelError("Name", "Naudotojas nurodytu vardu ar el. paštu jau egzistuoja.");
                    ModelState.AddModelError("Email", "Naudotojas nurodytu vardu ar el. paštu jau egzistuoja.");
                    TempData["RegisterStatus"] = "RegisterFailed";
                }

                if (!ModelState.IsValid)
                {
                    return View();
                }
                user.Admin = false;
                user.Name = user.Name.Trim();
                user.Password = _context.Helper.FromSql($"SELECT crypt({user.Password}, gen_salt('bf', 8));").ToList().FirstOrDefault().crypt;
                _context.Add(user);
                await _context.SaveChangesAsync();
                TempData["RegisterStatus"] = "RegisterSuccess";
            }
            
            return View();
        }
        // Users/ChangeRegistration
        //[ValidateAntiForgeryToken]
        //[HttpPost, ActionName("ChangeRegistration")]
        public async Task<IActionResult> ChangeRegistration()
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
                if (!user.Admin)
                {
                    return RedirectToAction("Index", "Home");
                }
                else
                {
                    if (Environment.GetEnvironmentVariable("RegisterEnabled") == "true")
                    {
                        Environment.SetEnvironmentVariable("RegisterEnabled", "false");
                    }
                    else
                        Environment.SetEnvironmentVariable("RegisterEnabled", "true");
                    return RedirectToAction("Details");

                }
            }
            catch (Exception ex)
            {
                TempData["AuthStatus"] = "AuthError";
                return RedirectToAction("Index", "Home");
            }
        }

        [Route("RegisterAdmin")]
        public IActionResult RegisterAdmin()
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
                    return View();
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

        // POST: RegisterAdmin
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [Route("RegisterAdmin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RegisterAdmin([Bind("Name,Email,Password")] User newUser)
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
                    if (GetUser(newUser.Name) != null)
                    {
                        ModelState.AddModelError("Name", "Naudotojas nurodytu vardu ar el. paštu jau egzistuoja.");
                    }
                    if (GetUserByEmail(newUser.Email) != null)
                    {
                        ModelState.AddModelError("Email", "Naudotojas nurodytu vardu ar el. paštu jau egzistuoja.");
                    }

                    if (!ModelState.IsValid)
                    {
                        TempData["RegisterStatus"] = "RegisterFailed";
                        return View();
                    }
                    newUser.Admin = true;
                    user.Name = newUser.Name.Trim();
                    newUser.Password = _context.Helper.FromSql($"SELECT crypt({newUser.Password}, gen_salt('bf', 8));").ToList().FirstOrDefault().crypt;
                    _context.Add(newUser);
                    await _context.SaveChangesAsync();
                    TempData["RegisterStatus"] = "RegisterSuccess";
                    ModelState.Clear();
                    return View();
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

        [Route("Login")]
        public IActionResult Login()
        {
            return View();
        }

        // POST: Login
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [Route("Login")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginDTO login)
        {
            login.Name = login.Name.Trim();
            User user = GetUser(login.Name);
            if (user == null)
            {
                ModelState.AddModelError("Name", "Naudotojas nurodytu vardu nerastas.");
                TempData["LoginStatus"] = "LoginFailed";
            }
            else if (user.FailedLogins >= 3 && user.BlockTime.AddSeconds(20).ToUniversalTime() > DateTime.Now.ToUniversalTime())
            {
                TempData["LoginBlocked"] = "true";
                TempData["LoginTime"] = ((int)(user.BlockTime.AddSeconds(20).ToUniversalTime() - DateTime.Now.ToUniversalTime()).TotalSeconds).ToString();
                return View();
            }
            else
            {
                string helper = _context.Helper.FromSql($"SELECT crypt({login.Password}, {user.Password});").ToList().FirstOrDefault().crypt;
                if (helper != user.Password)
                {
                    ModelState.AddModelError("Password", "Slaptažodis neteisingas.");
                    TempData["LoginStatus"] = "LoginFailed";
                    user.FailedLogins++;
                    if (user.FailedLogins >= 3)
                    {
                        user.BlockTime = DateTime.Now.ToUniversalTime();
                        TempData["LoginBlocked"] = "true";
                        TempData["LoginTime"] = ((int)(user.BlockTime.AddSeconds(20).ToUniversalTime() - DateTime.Now.ToUniversalTime()).TotalSeconds).ToString();
                    }
                    _context.Users.Update(user);
                    _context.SaveChanges();
                }
            } 

            if (!ModelState.IsValid)
            {
                return View();
            }

            
            //login successful
            user.FailedLogins = 0;
            _context.Users.Update(user);
            _context.SaveChanges();
            string cookie = _pmtsJwt.Create(user.Id);
            Response.Cookies.Append("userCookie", cookie, new CookieOptions
            {
                HttpOnly = true
            });

            TempData["LoginStatus"] = "LoginSuccess";
            TempData["AuthStatus"] = "AuthSuccess";
            
            
            return RedirectToAction("Index", "Home");
        }

        public IActionResult ChangePassword()
        {
            try
            {
                //tikrinama ar naudotojas prisijunges
                string cookie = Request.Cookies["userCookie"];
                JwtSecurityToken validatedToken = _pmtsJwt.Validate(cookie);
                User user = GetUser(int.Parse(validatedToken.Issuer));
                if (user == null)
                {
                    return RedirectToAction("Index", "Home");
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

        // POST: ChangePassword
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(PasswordDTO passwordDTO)
        {
            try
            {
                //tikrinama ar naudotojas prisijunges
                string cookie = Request.Cookies["userCookie"];
                JwtSecurityToken validatedToken = _pmtsJwt.Validate(cookie);
                User user = GetUser(int.Parse(validatedToken.Issuer));
                if (user == null)
                {
                    return RedirectToAction("Index", "Home");
                }
                else
                {
                    string oldPassword = _context.Helper.FromSql($"SELECT crypt({passwordDTO.oldPassword}, {user.Password});").ToList().FirstOrDefault().crypt;
                    string newPassword = _context.Helper.FromSql($"SELECT crypt({passwordDTO.newPassword}, gen_salt('bf', 8));").ToList().FirstOrDefault().crypt;
                    if (oldPassword != user.Password)
                    {
                        ModelState.AddModelError("oldPassword", "Slaptažodis neteisingas.");
                        TempData["ChangeStatus"] = "ChangeFailed";
                    }
                    if(passwordDTO.newPassword != passwordDTO.newPasswordConfirm)
                    {
                        ModelState.AddModelError("newPasswordConfirm", "Slaptažodžiai nesutampa.");
                        TempData["ChangeStatus"] = "ChangeFailed";
                    }
                    if (!ModelState.IsValid)
                    {
                        return View();
                    }

                    user.Password = newPassword;
                    _context.Update(user);
                    await _context.SaveChangesAsync();
                    TempData["ChangeStatus"] = "ChangeSuccess";
                    TempData["AuthStatus"] = "AuthSuccess";
                    return View();
                }
            }
            catch (Exception ex)
            {
                TempData["AuthStatus"] = "AuthError";
                return RedirectToAction("Index", "Home");
            }
        }

        [Route("Logout")]
        public IActionResult Logout()
        {
            try
            {
                string cookie = Request.Cookies["userCookie"];
                JwtSecurityToken validatedToken = _pmtsJwt.Validate(cookie);
                Response.Cookies.Delete("userCookie");
                TempData["LoginStatus"] = "LoggedOut";

                return RedirectToAction("Login");
            }
            catch (Exception ex)
            {
                TempData["AuthStatus"] = "AuthError";
                return RedirectToAction("Index", "Home");
            }

        }

        // GET: Users/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null || _context.Users == null)
            {
                return NotFound();
            }

            var user = await _context.Users.FindAsync(id);
            if (user == null)
            {
                return NotFound();
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
                if (!currentUser.Admin)
                {
                    return RedirectToAction("Index", "Home");
                }
                else
                {
                    return View(user);
                }
            }
            catch (Exception ex)
            {
                TempData["AuthStatus"] = "AuthError";
                return RedirectToAction("Index", "Home");
            }
        }

        // POST: Users/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Name,Email")] User user)
        {
            if (id != user.Id)
            {
                return NotFound();
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
                if (!currentUser.Admin)
                {
                    return RedirectToAction("Index", "Home");
                }
                else
                {

                    if (ModelState.IsValid)
                    {
                        try
                        {
                            _context.Update(user);
                            await _context.SaveChangesAsync();
                        }
                        catch (DbUpdateConcurrencyException)
                        {
                            if (!UserExists(user.Id))
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
                    return View(user);
                }
            }
            catch (Exception ex)
            {
                TempData["AuthStatus"] = "AuthError";
                return RedirectToAction("Index", "Home");
            }
        }

        // GET: Users/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null || _context.Users == null)
            {
                return NotFound();
            }

            var user = await _context.Users
                .FirstOrDefaultAsync(m => m.Id == id);
            if (user == null)
            {
                return NotFound();
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
                if (!currentUser.Admin)
                {
                    return RedirectToAction("Index", "Home");
                }
                else
                {
                    return View(user);
                }
            }
            catch (Exception ex)
            {
                TempData["AuthStatus"] = "AuthError";
                return RedirectToAction("Index", "Home");
            }

            
        }

        // POST: Users/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            if (_context.Users == null)
            {
                return Problem("Entity set 'PSQLcontext.Users'  is null.");
            }
            var user = await _context.Users.FindAsync(id);

            if (user != null)
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
                    if (!currentUser.Admin)
                    {
                        return RedirectToAction("Index", "Home");
                    }
                    else
                    {
                        List<Contestant> contestants = _context.Contestant.Where(c => c.UserId == user.Id).ToList();
                        foreach (var contestant in contestants)
                        {
                            List<Photo> photos = _context.Photo.Where(p => p.ContestantId == contestant.Id).ToList();
                            foreach (Photo photo in photos)
                            {
                                BlobClient blobClient = _blobContainerClient.GetBlobClient(photo.Name);
                                blobClient.Delete();
                                BlobClient blobClientThumb = _blobContainerClient.GetBlobClient(photo.ThumbName);
                                blobClientThumb.Delete();
                                _context.Photo.Remove(photo);
                            }
                            _context.Contestant.Remove(contestant);
                        }
                        

                        _context.Users.Remove(user);
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

        // GET: Users/Photos/5
        public async Task<IActionResult> Photos(int? id)
        {
            if (id == null || _context.Tournament == null || _context.Users == null)
            {
                return NotFound();
            }

            var photoUser = await _context.Users.FindAsync(id);
            if (photoUser == null)
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
                if (!user.Admin)
                {
                    return RedirectToAction("Index", "Home");
                }
                else
                {
                    List<Photo> userPhotos = new List<Photo>();
                    List<Contestant> contestants = _context.Contestant.Where(c => c.UserId == photoUser.Id).ToList();
                    foreach (var contestant in contestants)
                    {
                        List<Photo> photos = _context.Photo.Where(p => p.ContestantId == contestant.Id).ToList();
                        foreach (Photo photo in photos)
                        {
                            userPhotos.Add(photo);
                        }
                    }

                    return View(userPhotos);
                }
            }
            catch (Exception ex)
            {
                TempData["AuthStatus"] = "AuthError";
                return RedirectToAction("Privacy", "Home");
            }

        }

        // Users/DeleteUserPhoto/5
        //[ValidateAntiForgeryToken]
        //[HttpPost, ActionName("DeleteUserPhoto")]
        public async Task<IActionResult> DeleteUserPhoto(int? id)
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
                if (!user.Admin)
                {
                    return RedirectToAction("Index", "Home");
                }
                else
                {
                    Contestant contestant = _context.Contestant.FirstOrDefault(e => e.Id == photo.ContestantId);
                    BlobClient blobClient = _blobContainerClient.GetBlobClient(photo.Name);
                    blobClient.Delete();
                    BlobClient blobClientThumb = _blobContainerClient.GetBlobClient(photo.ThumbName);
                    blobClientThumb.Delete();
                    contestant.Points -= photo.Points;
                    _context.Photo.Remove(photo);
                    await _context.SaveChangesAsync();
                    return RedirectToAction("Index", "Users");

                }
            }
            catch (Exception ex)
            {
                TempData["AuthStatus"] = "AuthError";
                return RedirectToAction("Index", "Home");
            }
        }

        public bool UserExists(int id)
        {
          return (_context.Users?.Any(e => e.Id == id)).GetValueOrDefault();
        }

        public User GetUser(int id)
        {
            return _context.Users.FirstOrDefault(e => e.Id == id);
        }
        public User GetUser(string name)
        {
            return _context.Users.FirstOrDefault(e => e.Name == name);
        }
        public User GetUserByEmail(string email)
        {
            return _context.Users.FirstOrDefault(e => e.Email == email);
        }
    }
}
