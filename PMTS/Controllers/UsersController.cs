using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using PMTS.Authentication;
using PMTS.DTOs;
using PMTS.Models;

namespace PMTS.Controllers
{
    public class UsersController : Controller
    {
        private readonly PSQLcontext _context;
        private readonly PmtsJwt _pmtsJwt;

        public UsersController(PSQLcontext context, PmtsJwt pmtsJwt)
        {
            _context = context;
            _pmtsJwt = pmtsJwt;
        }

        // GET: Users
        public async Task<IActionResult> Index()
        {
              return _context.Users != null ? 
                          View(await _context.Users.ToListAsync()) :
                          Problem("Entity set 'PSQLcontext.Users'  is null.");
        }

        // GET: Users/Details
        public async Task<IActionResult> Details()
        {
            try
            {
                string cookie = Request.Cookies["userCookie"];
                JwtSecurityToken validatedToken = _pmtsJwt.Validate(cookie);
                User user = GetUser(int.Parse(validatedToken.Issuer));
                return View(user);
            }
            catch (Exception ex)
            {
                TempData["AuthStatus"] = "AuthError";
                return RedirectToAction("Login");
            }
        }


        [Route("Register")]
        public IActionResult Register()
        {
            return View();
        }

        // POST: Register
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [Route("Register")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register([Bind("Id,Name,Email,Password")] User user)
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
                //_context.Add(user);
                //await _context.SaveChangesAsync();
                //return RedirectToAction(nameof(Index));
                return View();
            }
            //psql for salt: UPDATE public."Users" Set "Password" = crypt('slaptazodis123', gen_salt('bf')) WHERE public."Users"."Id" = 1;
            user.Admin = false;
            //user.Password = _context.Helper.FromSql($"SELECT crypt('{user.Password}', gen_salt('bf', 8));").ToList().FirstOrDefault().crypt;
            _context.Add(user);
            await _context.SaveChangesAsync();
            TempData["RegisterStatus"] = "RegisterSuccess";
            return View();
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
            User user = GetUser(login.Name);
            if (user == null)
            {
                ModelState.AddModelError("Name", "Naudotojas nurodytu vardu nerastas.");
                TempData["LoginStatus"] = "LoginFailed";
            }
            else
            {
                //Helper helper = _context.Helper.FromSql($"SELECT crypt('{login.Password}', '{user.Password}');").FirstOrDefault();
                //if (helper.crypt != user.Password)
                //{
                //    ModelState.AddModelError("Password", "Slaptažodis neteisingas.");
                //    TempData["LoginStatus"] = "LoginFailed";
                //    TempData["login"] = login.Password;
                //    TempData["user"] = user.Password;
                //    TempData["crypt"] = helper.crypt;
                //    TempData["test"] = "hello";
                //}
                if (user.Password != login.Password)
                {
                    ModelState.AddModelError("Password", "Slaptažodis neteisingas.");
                    TempData["LoginStatus"] = "LoginFailed";
                }
            } 

            if (!ModelState.IsValid)
            {
                //_context.Add(user);
                //await _context.SaveChangesAsync();
                //return RedirectToAction(nameof(Index));
                return View();
            }
            //psql for salt: UPDATE public."Users" Set "Password" = crypt('slaptazodis123', gen_salt('bf')) WHERE public."Users"."Id" = 1;

            string cookie = _pmtsJwt.Create(user.Id);
            Response.Cookies.Append("userCookie", cookie, new CookieOptions
            {
                HttpOnly = true
            });

            TempData["LoginStatus"] = "LoginSuccess";
            TempData["AuthStatus"] = "AuthSuccess";
            return RedirectToAction("Index", "Tournaments");
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
                return RedirectToAction("Login");
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
            return View(user);
        }

        // POST: Users/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Name,Email,Password")] User user)
        {
            if (id != user.Id)
            {
                return NotFound();
            }

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

            return View(user);
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
                _context.Users.Remove(user);
            }
            
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
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
