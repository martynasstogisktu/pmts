using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PMTS.Authentication;
using PMTS.Models;
using System.Diagnostics;
using System.IdentityModel.Tokens.Jwt;

namespace PMTS.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly PSQLcontext _context;
        private readonly PmtsJwt _pmtsJwt;

        public HomeController(ILogger<HomeController> logger, PSQLcontext context, PmtsJwt pmtsJwt)
        {
            _logger = logger;
            _context = context;
            _pmtsJwt = pmtsJwt;
        }

        public IActionResult Index()
        {
            try
            {
                string cookie = Request.Cookies["userCookie"];
                JwtSecurityToken validatedToken = _pmtsJwt.Validate(cookie);
                User user = _context.Users.FirstOrDefault(e => e.Id == int.Parse(validatedToken.Issuer));
                if (user != null)
                {
                    if (user.Admin)
                    {
                        TempData["IsAdmin"] = "true";
                    }
                    else
                        TempData["IsAdmin"] = "false";
                }
                else
                {
                    TempData["LoggedIn"] = "false";
                }
                return View();
            }
            catch (Exception ex)
            {
                TempData["LoggedIn"] = "false";
                return View();
            }
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}