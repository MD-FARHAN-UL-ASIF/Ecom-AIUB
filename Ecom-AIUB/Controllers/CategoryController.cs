using Ecom_AIUB.EF;
using Ecom_AIUB.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Ecom_AIUB.Controllers
{
    public class CategoryController : Controller
    {
        private readonly ILogger<CategoryController> _logger;
        private readonly DataContext _db;

        public CategoryController(ILogger<CategoryController> logger, DataContext data)
        {
            _logger = logger;
            _db = data;
        }

        [HttpGet]
        [Route("category/index")]
        public async Task<IActionResult> Index()
        {
            var categories = await _db.Category.ToListAsync();

            if (categories != null && categories.Any())
            {
                return View(categories);
            }
            else
            {
                ViewBag.Message = "There are no categories.";
                return View();
            }
        }

        [HttpGet]
        [Route("category/create")]
        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [Route("category/create")]
        public async Task<IActionResult> Create(Category category)
        {
            if (ModelState.IsValid)
            {
                _db.Category.Add(category);
                await _db.SaveChangesAsync();
                return RedirectToAction("Index");
            }
            return View(category);
        }

        [HttpGet]
        [Route("category/edit/{id}")]
        public async Task<IActionResult> Edit(int id)
        {
            var category = await _db.Category.FindAsync(id);
            if (category == null)
            {
                return NotFound();
            }
            return View(category);
        }

        [HttpPost]
        [Route("category/edit/{id}")]
        public async Task<IActionResult> Edit(int id, Category category)
        {
            if (id != category.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _db.Update(category);
                    await _db.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!CategoryExists(category.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction("Index");
            }
            return View(category);
        }

        [HttpGet]
        [Route("category/delete/{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var category = await _db.Category.FindAsync(id);
            if (category == null)
            {
                return NotFound();
            }
            return View(category);
        }

        [HttpPost, ActionName("Delete")]
        [Route("category/delete/{id}")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var category = await _db.Category.FindAsync(id);
            if (category != null)
            {
                _db.Category.Remove(category);
                await _db.SaveChangesAsync();
            }
            return RedirectToAction("Index");
        }

        private bool CategoryExists(int id)
        {
            return _db.Category.Any(e => e.Id == id);
        }
    }
}
