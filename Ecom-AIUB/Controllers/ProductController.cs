using Ecom_AIUB.EF;
using Ecom_AIUB.Models;
using Ecom_AIUB.Models.DTOs;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;

namespace Ecom_AIUB.Controllers
{
    public class ProductController : Controller
    {
        private readonly ILogger<ProductController> _logger;
        private readonly DataContext _db;
        private readonly IWebHostEnvironment webHostEnvironment;

        public ProductController(ILogger<ProductController> logger, DataContext db, IWebHostEnvironment webHostEnvironment)
        {
            _logger = logger;
            _db = db;
            this.webHostEnvironment = webHostEnvironment;
        }

        [HttpGet]
        [Route("/products")]
        public IActionResult Index()
        {
            return View();
        }

        [HttpGet]
        [Route("/products/{id}")]
        public IActionResult Details(int id)
        {
            return View();
        }

        [HttpGet]
        [Route("/products/create")]
        public IActionResult Create()
        {
            var categories = _db.Category.ToList();
            return View(categories);
        }

        [HttpPost]
        [Route("/products/create")]
        public async Task<IActionResult> Create(ProductDTO data)
        {
            string FileName = UploadedFile(data);

            if (data == null)
            {
                ViewBag.ErrMessage = "Please enter the required fields";
                return View();
            }
            else
            {

                if (!ModelState.IsValid) 
                {
                    var product = new Product()
                    {
                        Name = data.Name,
                        Description = data.Description,
                        Price = data.Price,
                        Manufacturer = data.Manufacturer,
                        Category_Id = data.Category_Id,
                        Quantity = data.Quantity,
                        Image = FileName
                    };

                    _db.Products.Add(product);
                    if (await _db.SaveChangesAsync() > 0)
                    {
                        return RedirectToAction("Index","Home");
                    }
                    else 
                    {
                        ViewBag.ErrMessage = "Something went wrong! There was a problem storing the data.";
                        return View();
                    }
                }
            }
            return View();
        }
        public IActionResult Edit()
        {
            return View();
        }
        public IActionResult Delete()
        {
            return View();
        }

        private string UploadedFile(ProductDTO data)
        {
            string uniqueFileName = null;

            if (data.Image != null && data.Image.Length > 0)
            {
                string uploadsFolder = Path.Combine(webHostEnvironment.WebRootPath, "images");
                uniqueFileName = Guid.NewGuid().ToString() + "_" + data.Image.FileName;
                string filePath = Path.Combine(uploadsFolder, uniqueFileName);
                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    data.Image.CopyTo(fileStream);
                }
            }
            return uniqueFileName;
        }
    }
}
