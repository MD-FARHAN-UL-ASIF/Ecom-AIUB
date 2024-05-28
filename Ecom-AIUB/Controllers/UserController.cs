    using Ecom_AIUB.EF;
    using Ecom_AIUB.Models;
    using Ecom_AIUB.Models.DTOs;
    using Microsoft.AspNetCore.Authentication;
    using Microsoft.AspNetCore.Authentication.Cookies;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.Extensions.Logging;
    using System.Collections.Generic;
    using System.Linq;
    using System.Security.Claims;
    using System.Threading.Tasks;

    namespace Ecom_AIUB.Controllers
    {
        public class UserController : Controller
        {
            private readonly ILogger<UserController> _logger;
            private readonly DataContext _db;

            public UserController(ILogger<UserController> logger, DataContext data)
            {
                _logger = logger;
                _db = data;
            }

            public IActionResult Index()
            {
                return View();
            }

            [HttpGet]
            [Route("/signup")]
            public IActionResult SignUp()
            {
                return View();
            }

            [HttpPost]
            [Route("/signup")]
            public async Task<IActionResult> SignUp(UserDTO data)
            {
                var user = new User()
                {
                    Name = data.Name,
                    Email = data.Email,
                    Password = data.Password,
                    PhoneNumber = data.PhoneNumber,
                    UserType = 2
                };

                _db.Users.Add(user);
                await _db.SaveChangesAsync();

                return RedirectToAction("Login");
            }

            [HttpGet]
            [Route("/login")]
            public IActionResult Login()
            {
                return View();
            }

            [HttpPost]
            [Route("/login")]
            public async Task<IActionResult> Login(UserDTO data)
            {
                var user = _db.Users.FirstOrDefault(x => x.Email == data.Email && x.Password == data.Password);

                if (user != null)
                {
                    var claims = new List<Claim>
                    {
                        new Claim(ClaimTypes.Name, user.Name),
                        new Claim(ClaimTypes.Email, user.Email),
                        new Claim("UserType", user.UserType.ToString())
                    };

                    var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

                    await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(claimsIdentity));

                    if (user.UserType == 1)
                    {
                        HttpContext.Session.SetString("email", user.Email);
                        return RedirectToAction("AdminDashboard", "Home");
                    }
                    else
                    {
                        HttpContext.Session.SetString("email", user.Email);
                        return RedirectToAction("Index", "Home");
                    }
                }
                else
                {
                    ViewBag.Message = "User Not Found :(";
                    return View();
                }
            }

        [HttpGet]
        [Route("/logout")]
        public async Task<IActionResult> Logout()
        {
            HttpContext.Session.Clear();
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Index", "Home"); 
        }


        public IActionResult AccessDenied()
            {
                return View();
            }
        }
    }
