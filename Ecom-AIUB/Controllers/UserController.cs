using Ecom_AIUB.EF;
using Ecom_AIUB.Models;
using Ecom_AIUB.Models.DTOs;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
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

        [HttpGet]
        [Route("/users")]
        [Authorize(Policy = "AdminOnly")]
        public async Task<IActionResult> Index()
        {
            var users = await _db.Users
                .Select(u => new
                {
                    u.Id,
                    u.Name,
                    u.Email,
                    LatestAddress = _db.Addresses
                        .Where(a => a.UserId == u.Id)
                        .OrderByDescending(a => a.Id)
                        .FirstOrDefault()
                })
                .ToListAsync();

            return View(users);
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

        [HttpGet]
        [Authorize(Policy = "UserOnly")]
        [Route("/edit/profile")]
        public async Task<IActionResult> EditProfile()
        {
            var userEmail = User.FindFirstValue(ClaimTypes.Email);

            var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == userEmail);

            if (user == null)
            {
                return NotFound();
            }

            var userDTO = new UserDTO
            {
                Id = user.Id,
                Name = user.Name,
                Email = user.Email,
                PhoneNumber = user.PhoneNumber,
                UserType = user.UserType
            };

            return View(userDTO);
        }

        [HttpPost]
        [Route("/edit/profile")]
        public async Task<IActionResult> EditProfile(UserDTO updatedUserData)
        {
            try
            {
                // Find the user by Id
                var user = await _db.Users.FindAsync(updatedUserData.Id);

                if (user == null)
                {
                    return NotFound("User not found."); // Return 404 with an error message
                }

                // Update user properties
                user.Name = updatedUserData.Name;
                user.PhoneNumber = updatedUserData.PhoneNumber;

                // Save changes to the database
                await _db.SaveChangesAsync();

                // Redirect to a profile page or any other appropriate page
                return RedirectToAction("Index", "Home");
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Error occurred while updating user profile.");
                ModelState.AddModelError("", "An error occurred while updating your profile. Please try again later.");
                return View(updatedUserData); // Return the view with the updated data and error message
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error occurred while updating user profile.");
                return StatusCode(500, "Internal server error."); // Return 500 Internal Server Error with a generic message
            }
        }




        public IActionResult AccessDenied()
        {
            return View();
        }
    }
}
