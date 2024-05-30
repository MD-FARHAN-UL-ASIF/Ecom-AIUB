using Ecom_AIUB.EF;
using Ecom_AIUB.Models;
using Ecom_AIUB.Models.DTOs;
using Microsoft.AspNetCore.Authorization;
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
        [Authorize(Policy = "AdminOnly")]
        public async Task<IActionResult> Index()
        {
            var products = await _db.Products.Include(p => p.Category).ToListAsync();
            return View(products);
        }

        [HttpGet]
        [Route("/product/{id}")]
        public async Task<IActionResult> Details(int id)
        {
            var product = await _db.Products.Where(x => x.Id == id).Include(x => x.Category).FirstOrDefaultAsync();
            if (product != null)
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
        [Authorize(Policy = "AdminOnly")]
        public IActionResult Create()
        {
            var categories = _db.Category.ToList();
            return View(categories);
        }

        [HttpPost]
        [Route("/products/create")]
        [Authorize(Policy = "AdminOnly")]
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
                        return RedirectToAction("Index", "Home");
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
        [HttpGet]
        [Route("/products/edit/{id}")]
        [Authorize(Policy = "AdminOnly")]
        public async Task<IActionResult> Edit(int id)
        {
            var product = await _db.Products.FindAsync(id);
            if (product == null)
            {
                ViewBag.ErrMessage = "Product not found!";
                return View();
            }

            var categories = await _db.Category.ToListAsync();
            var productDTO = new ProductDTO
            {
                Id = product.Id,
                Name = product.Name,
                Description = product.Description,
                Price = product.Price,
                Manufacturer = product.Manufacturer,
                Category_Id = product.Category_Id,
                Quantity = product.Quantity,
                ExistingImage = product.Image
            };

            ViewBag.Categories = categories;
            return View(productDTO);
        }

        [HttpPost]
        [Route("/products/edit/{id}")]
        [Authorize(Policy = "AdminOnly")]
        public async Task<IActionResult> Edit(int id, ProductDTO data)
        {
                var product = await _db.Products.FindAsync(data.Id);
                if (product != null)
                {
                    product.Name = data.Name;
                    product.Description = data.Description;
                    product.Price = data.Price;
                    product.Manufacturer = data.Manufacturer;
                    product.Category_Id = data.Category_Id;
                    product.Quantity = data.Quantity;

                    if (data.Image != null)
                    {
                        if (!string.IsNullOrEmpty(data.ExistingImage))
                        {
                            string filePath = Path.Combine(webHostEnvironment.WebRootPath, "images", data.ExistingImage);
                            if (System.IO.File.Exists(filePath))
                            {
                                System.IO.File.Delete(filePath);
                            }
                        }
                        product.Image = UploadedFile(data);
                    }

                    _db.Products.Update(product);
                    await _db.SaveChangesAsync();

                    return RedirectToAction("Index", "Product");
                }

            ViewBag.Categories = await _db.Category.ToListAsync();
            return View(data);
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

        [HttpGet]
        [Route("/price/low")]
        public async Task<JsonResult> PriceToLow()
        {
            var products = await _db.Products.OrderBy(x => x.Price).ToListAsync();
            return Json(products);
        }

        [HttpGet]
        [Route("/price/high")]
        public async Task<JsonResult> PriceToHigh()
        {
            var products = await _db.Products.OrderByDescending(x => x.Price).ToListAsync();
            return Json(products);
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
