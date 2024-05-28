using Ecom_AIUB.EF;
using Ecom_AIUB.Models;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Ecom_AIUB.Controllers
{
    public class CartController : Controller
    {
        private readonly ILogger<CartController> _logger;
        private readonly DataContext db;

        public CartController(DataContext data, ILogger<CartController> logger) 
        {
            _logger = logger;
            db = data;
        }

        [HttpGet]
        [Route("/cart")]
        public IActionResult Index()
        {
            var email = HttpContext.Session.GetString("email");
            if (email == null)
            {
                return RedirectToAction("login", "Home");
            }

            var user = db.Users.FirstOrDefault(x => x.Email == email);
            if (user == null)
            {
                return RedirectToAction("login", "Home");
            }

            var cartItems = db.Carts
                .Include(x => x.User)
                .Include(x => x.Product)
                .Where(x => x.UserId == user.Id)
                .ToList();

            return View(cartItems);
        }

        [HttpGet]
        [Route("/cart/add/{id}")]
        public async Task<IActionResult> Create(int id)
        {
            var email = HttpContext.Session.GetString("email");
            if (email != null)
            {
                var user = db.Users.Where(x => x.Email == email).FirstOrDefault();
                if (user != null) 
                {
                    var product = await db.Products.FindAsync(id);
                    if (product != null && product.Quantity > 0)
                    {
                        var cart = await db.Carts.Where(x => x.ProductId == id).FirstOrDefaultAsync();
                        if (cart != null)
                        {
                            cart.Quantity += 1;
                            int currentQuantity = cart.Quantity;
                            cart.Price = product.Price * currentQuantity;
                            db.Update(cart);
                            await db.SaveChangesAsync();
                            return RedirectToAction("Index");
                        }
                        else
                        {
                            cart = new Cart()
                            {
                                UserId = user.Id,
                                ProductId = id,
                                Quantity = 1,
                                Price = product.Price,
                            };
                            await db.AddAsync(cart);
                            await db.SaveChangesAsync();
                            return RedirectToAction("Index");
                        }
                    }
                    else
                    {
                        return RedirectToAction("Index", "Home");
                    }
                }
                else
                {
                    return RedirectToAction("Login", "User");
                }
            }
            else
            {
                return RedirectToAction("Login", "User");
            }
        }

        [HttpGet]
        [Route("/cart/increase/{id}")]
        public async Task<JsonResult> IncreaseCartItem(int id)
        {
            var item = await db.Carts.FindAsync(id);
            if (item != null)
            {
                var product = await db.Products.Where(x => x.Id == item.ProductId).FirstOrDefaultAsync();
                if (product != null) 
                {
                    if(item.Quantity < product.Quantity)
                    {
                        var currentQuantity = item.Quantity += 1;
                        item.Price = product.Price * currentQuantity;

                        db.Carts.Update(item);
                        await db.SaveChangesAsync();
                        return Json(new { 
                            success = true,
                            message = "Cart updated.",
                            data = item
                        });
                    }
                    else
                    {
                        return Json(new
                        {
                            success = false,
                            message = "Out of stock.",
                        });
                    }
                }
                else
                {
                    return Json(new
                    {
                        success = false,
                        message = "Product not found.",
                    });
                }
            }
            return Json(new { 
                success = false, 
                message = "update failed",
            });
        }

        [HttpGet]
        [Route("/cart/decrease/{id}")]
        public async Task<JsonResult> DecreaseCartItem(int id)
        {
            var item = await db.Carts.FindAsync(id);
            if (item != null)
            {
                var product = await db.Products.Where(x => x.Id == item.ProductId).FirstOrDefaultAsync();
                if (product != null)
                {
                    if (item.Quantity > 1)
                    {
                        var currentQuantity = item.Quantity -= 1;
                        item.Price = product.Price * currentQuantity;

                        db.Carts.Update(item);
                        await db.SaveChangesAsync();
                        return Json(new
                        {
                            success = true,
                            message = "Cart updated.",
                            data = item
                        });
                    }
                    else if (item.Quantity == 1)
                    {
                        db.Carts.Remove(item);
                        await db.SaveChangesAsync();

                        return Json(new
                        {
                            success = true,
                            message = "Item removed from cart.",
                        });
                    }
                    else
                    {
                        return Json(new
                        {
                            success = false,
                            error = "Quantity cannot be negative."
                        });
                    }
                }
                else
                {
                    return Json(new
                    {
                        success = false,
                        error = "Product Not found."
                    });
                }
            }
            return Json(new
            {
                success = false,
                error = "Item is not in the cart."
            });
        }

        [HttpGet]
        [Route("/cart/remove/{id}")]
        public async Task<JsonResult> RemoveFromCart(int id)
        {
            var item = await db.Carts.FindAsync(id);

            if (item != null)
            {
                db.Carts.Remove(item);
                if(await db.SaveChangesAsync() > 0)
                {
                    return Json(new
                    {
                        success = true,
                        message = "Item removed from cart"
                    });
                }
                
            }

            return Json(new
            {
                success = false,
                message = "Item not found!"

            });
        }
    }
}
