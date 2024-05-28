using Ecom_AIUB.EF;
using Ecom_AIUB.Models;
using Ecom_AIUB.Models.DTOs;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

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
        [Route("/product/{id}")]
        public async Task<IActionResult> Details(int id)
        {
            var product = await _db.Products.Where(x => x.Id == id).Include(x => x.Category).FirstOrDefaultAsync();
            if(product != null)
            {
                var products = await _db.Products.Where(x => x.Category_Id == product.Category_Id).ToListAsync();

                var data = new
                {
                    productData = product,
                    allProducts = products
                };

                return View(data);
            }
            ViewBag.ErrMessage = "Unable to find the product!!";
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

        [HttpGet]
        [Route("/category/products/{id}")]
        public async Task<IActionResult> ProductByCategory(int id)
        {
            var category = await _db.Category.FindAsync(id);
            var categories = await _db.Category.ToListAsync();
            if (category != null)
            {
                var products = await _db.Products.Where(x => x.Category_Id == id).ToListAsync();
                if (products.Count > 0)
                {
                    var data = new
                    {
                        categoryData = category,
                        categoriesData = categories,
                        productsData = products
                    };
                    return View(data);
                }
            }
            ViewBag.ErrMessage = "Something went wrong!";
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
