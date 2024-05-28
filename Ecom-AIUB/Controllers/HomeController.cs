using Ecom_AIUB.EF;
using Ecom_AIUB.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Threading.Tasks;

namespace Ecom_AIUB.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly DataContext _db;

        public HomeController(ILogger<HomeController> logger, DataContext db)
        {
            _logger = logger;
            _db = db;
        }

        [HttpGet]
        [Route("/")]
        public async Task<IActionResult> Index()
        {
            var products = await _db.Products.Include(p => p.Category).ToListAsync();
            if (products.Count > 0)
            {
                var groupedProducts = products.GroupBy(p => new { p.Category.Id, p.Category.Name })
                                              .Select(g => new { CategoryId = g.Key.Id, CategoryName = g.Key.Name, Products = g.ToList() })
                                              .ToList();

                return View(groupedProducts);
            }
            else
            {
                ViewBag.ProductMessage = "No products available";
                return View();
            }
        }

        [HttpGet]
        [Route("/user")]
        [Authorize(Policy = "UserOnly")]
        public IActionResult UserDashboard()
        {
            return View();
        }

        [HttpGet]
        [Route("/admin")]
        [Authorize(Policy = "AdminOnly")]
        public IActionResult AdminDashboard()
        {
            return View();
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
