using Ecom_AIUB.EF;
using Ecom_AIUB.Models;
using Ecom_AIUB.Models.DTOs;
using Ecom_AIUB.PaymentGateway;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Collections.Specialized;
using System.Text.Json;
using static Ecom_AIUB.PaymentGateway.SSLCommerz;

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
        [Authorize(Policy = "UserOnly")]

        public IActionResult Index()
        {
            var email = HttpContext.Session.GetString("email");
            if (email == null)
            {
                return RedirectToAction("login", "User");
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

            //add address
            var address = new Address()
            {
                ShippingAddress = data.Address,
                City = data.City,
                PostCode = data.PostCode,
                UserId = user.Id
            };

            await db.Addresses.AddAsync(address);
            await db.SaveChangesAsync();

            var carts = await db.Carts.Where(x => x.UserId == user.Id).ToListAsync();
            if (carts.Count <= 0)
            {
                return RedirectToAction("Index", "Home");
            }

            var productsName = new List<string>();
            var categoryId = new List<int>();
            var productId = new List<int>();

            decimal price = 0M;

            foreach (var cart in carts)
            {
                var product = await db.Products.FirstOrDefaultAsync(p => p.Id == cart.ProductId);
                if (product != null)
                {
                    price += product.Price;
                    productsName.Add(product.Name);
                    categoryId.Add(product.Category_Id);
                    productId.Add(product.Id);
                }
            }

            var trx_id = new Guid();

            var baseUrl = Request.Scheme + "://" + Request.Host;

            // CREATING LIST OF POST DATA
            // Creating list of post data
            var PostData = new NameValueCollection
            {
                { "total_amount", price.ToString("F2") },
                { "tran_id", trx_id.ToString() },
                { "success_url", $"{baseUrl}/payment/success" },
                { "fail_url", $"{baseUrl}/payment/fail" },
                { "cancel_url", $"{baseUrl}/" },
                { "version", "3.00" },
                { "cus_name", user.Name },
                { "cus_email", user.Email },
                { "cus_add1", data.Address },
                { "cus_add2", "Address Line Two" },
                { "cus_city", data.City },
                { "cus_state", "State Name" },
                { "cus_postcode", data.PostCode },
                { "cus_country", "Bangladesh" },
                { "cus_phone", user.PhoneNumber },
                { "cus_fax", "0171111111" },
                { "ship_name", "ABC XY" },
                { "ship_add1", "Address Line One" },
                { "ship_add2", "Address Line Two" },
                { "ship_city", "City Name" },
                { "ship_state", "State Name" },
                { "ship_postcode", "Post Code" },
                { "ship_country", "Country" },
                { "value_a", user.Id.ToString() },
                { "value_b", productId.ToString() },
                { "value_c", data.City },
                { "value_d", data.PostCode },
                { "shipping_method", "NO" },
                { "num_of_item", carts.Count.ToString() },
                { "product_name", string.Join(", ", productsName) },
                { "product_profile", "general" },
                { "product_category", categoryId.ToString() }
            };

            //we can get from email notificaton
            var storeId = "bootc6608eba3cb6dc";
            var storePassword = "bootc6608eba3cb6dc@ssl";
            var isSandboxMood = true;

            SSLCommerz sslcz = new SSLCommerz(storeId, storePassword, isSandboxMood);

            string response = response = sslcz.InitiateTransaction(PostData);

            Console.WriteLine($"Redirect URL: {response}");

            return Redirect(response);
        }

        [HttpPost]
        [Route("/payment/success")]
        public async Task<IActionResult> Success(SSLCommerzValidatorResponse data)
        {
            if (string.IsNullOrWhiteSpace(data.status) || data.status.ToString() != "VALID")
            {
                ViewBag.SuccessInfo = "There was an error while processing your payment. Please try again.";
                return View();
            }

            int user_id = Convert.ToInt32(data.value_a);

            var user = await db.Users.FirstOrDefaultAsync(x => x.Id == user_id);
            if (user == null)
            {
                return RedirectToAction("Login", "User");
            }

            HttpContext.Session.SetString("email", user.Email);

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

            string TrxID = data.tran_id.ToString();
            string amount = price.ToString();
            string currency = "BDT";

            var storeId = "bootc6608eba3cb6dc";
            var storePassword = "bootc6608eba3cb6dc@ssl";

            SSLCommerz sslcz = new SSLCommerz(storeId, storePassword, true);
            var response = sslcz.OrderValidate(TrxID, amount, currency, Request);

            var jsonData = JsonSerializer.Serialize(data);
            decimal orderAmount = Decimal.Parse(data.amount);

            string stringValue = data.value_b;
            List<int> productIds = new List<int>();

            _logger.LogInformation($"Raw value_b data: {stringValue}");

            if (!string.IsNullOrEmpty(stringValue))
            {
                string[] parts = stringValue.Split(',');
                foreach (string part in parts)
                {
                    if (int.TryParse(part.Trim(), out int intValue))
                    {
                        productIds.Add(intValue);
                    }
                    else
                    {
                        _logger.LogError($"Failed to parse product ID from value_b: {part}");
                    }
                }
            }

            _logger.LogInformation($"Parsed product IDs: {string.Join(", ", productIds)}");

            var address = await db.Addresses
                                .Where(x => x.UserId == user.Id)
                                .OrderByDescending(x => x.Id)
                                .FirstOrDefaultAsync();

            if (address == null)
            {
                return RedirectToAction("Index", "Home");
            }

            if (response)
            {
                var order = new Order()
                {
                    CustomerName = user.Name,
                    CustomerEmail = user.Email,
                    TransactionId = data.tran_id,
                    Amount = orderAmount,
                    OrderDate = DateTime.Parse(data.tran_date),
                    Response = jsonData,
                    ProductsId = productIds,
                    AddressId = address.Id,
                };

                await db.Orders.AddAsync(order);

                if (await db.SaveChangesAsync() > 0)
                {
                    db.Carts.RemoveRange(carts);
                    await db.SaveChangesAsync();
                }
            }

            var successInfo = $"Validation Response: {response}";
            ViewBag.SuccessInfo = successInfo;
            return View();
        }



        [Route("/payment/fail")]
        public IActionResult Fail()
        {
            return View();
        }

        [HttpGet]
        [Route("/cart/count")]
        public IActionResult GetCartItemCount()
        {
            var email = HttpContext.Session.GetString("email");
            if (email == null)
            {
                return Json(new { count = 0 });
            }

            var user = db.Users.FirstOrDefault(x => x.Email == email);
            if (user == null)
            {
                return Json(new { count = 0 });
            }

            var itemCount = db.Carts.Count(x => x.UserId == user.Id);
            return Json(new { count = itemCount });
        }

    }
}
