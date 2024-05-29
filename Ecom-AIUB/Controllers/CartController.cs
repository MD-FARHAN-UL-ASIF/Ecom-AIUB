using Ecom_AIUB.EF;
using Ecom_AIUB.Models;
using Ecom_AIUB.Models.DTOs;
using Ecom_AIUB.PaymentGateway;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Nancy;
using System.Collections.Specialized;

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
                        var cart = await db.Carts.Where(x => x.ProductId == id && x.UserId == user.Id).FirstOrDefaultAsync();
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

        [HttpPost]
        [Route("/checkout")]
        public async Task<IActionResult> Checkout(UserDTO data)
        {
            var email = HttpContext.Session.GetString("email");
            if(email == null)
            {
                return RedirectToAction("Login", "Home");
            }

            var user = await db.Users.Where(x => x.Email == email).FirstOrDefaultAsync();
            if (user == null) 
            {
                return RedirectToAction("Login", "Home");
            }

            var carts = await db.Carts.Where(x => x.UserId == user.Id).ToListAsync();
            if (carts.Count <= 0)
            {
                return RedirectToAction("Index", "Home");
            }

            var productsName = new List<string>();

            decimal price = 0M;

            foreach (var cart in carts)
            {
                var product = await db.Products.FirstOrDefaultAsync(p => p.Id == cart.ProductId);
                if (product != null)
                {
                    price += product.Price;
                    productsName.Add(product.Name);
                }
            }

            var trx_id = new Guid();

            var baseUrl = Request.Scheme + "://" + Request.Host;

            // CREATING LIST OF POST DATA
            NameValueCollection PostData = new NameValueCollection();

            PostData.Add("total_amount", $"{price}");
            PostData.Add("tran_id", $"{trx_id}");
            PostData.Add("success_url", baseUrl + "/payment/success");
            PostData.Add("fail_url", baseUrl + "/Cart/CheckoutFail");
            PostData.Add("cancel_url", baseUrl + "/Cart/CheckoutCancel");

            PostData.Add("version", "3.00");
            PostData.Add("cus_name", $"{user.Name}");
            PostData.Add("cus_email", $"{user.Email}");
            PostData.Add("cus_add1", $"{data.Address}");
            PostData.Add("cus_add2", "Address Line Tw");
            PostData.Add("cus_city", $"{data.City}");
            PostData.Add("cus_state", "State Nam");
            PostData.Add("cus_postcode", $"{data.PostCode}");
            PostData.Add("cus_country", "Bangladesh");
            PostData.Add("cus_phone", $"{user.PhoneNumber}");
            PostData.Add("cus_fax", "0171111111");
            PostData.Add("ship_name", "ABC XY");
            PostData.Add("ship_add1", "Address Line On");
            PostData.Add("ship_add2", "Address Line Tw");
            PostData.Add("ship_city", "City Nam");
            PostData.Add("ship_state", "State Nam");
            PostData.Add("ship_postcode", "Post Cod");
            PostData.Add("ship_country", "Countr");
            PostData.Add("value_a", $"{user.Id}");
            PostData.Add("value_b", "ref00");
            PostData.Add("value_c", "ref00");
            PostData.Add("value_d", "ref00");
            PostData.Add("shipping_method", "NO");
            PostData.Add("num_of_item", $"{carts.Count}");
            PostData.Add("product_name", $"{productsName}");
            PostData.Add("product_profile", "general");
            PostData.Add("product_category", "Demo");

            //we can get from email notificaton
            var storeId = "bootc6608eba3cb6dc";
            var storePassword = "bootc6608eba3cb6dc@ssl";
            var isSandboxMood = true;

            SSLCommerz sslcz = new SSLCommerz(storeId, storePassword, isSandboxMood);

            string response = sslcz.InitiateTransaction(PostData);

            return Redirect(response);
        }

        [HttpGet]
        [Route("/payment/success")]
        public IActionResult Success()
        {
            return View();
        }

        [HttpPost]
        [Route("/payment/success")]
        public IActionResult Success(string data) 
        { 
            return View(data); 
        }

        [Route("/payment/fail")]
        public IActionResult Fail()
        {
            return View();
        }
    }
}
