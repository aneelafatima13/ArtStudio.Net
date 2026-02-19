using BizOne.Common;
using BizOne.DAL;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace BizOne.Areas.EndUser.Controllers
{
    public class CartController : Controller
    {
        private readonly ProductsDAL productDal = new ProductsDAL();

        [HttpPost]
        public JsonResult AddToCart(long id, int quantity)
        {
            // 1. Get current cart from session or create new one
            var cart = Session["Cart"] as List<CartItem> ?? new List<CartItem>();

            // 2. Check if product already exists in cart
            var existingItem = cart.FirstOrDefault(x => x.ProductId == id);
            if (existingItem != null)
            {
                existingItem.Quantity += quantity;
            }
            else
            {
                cart.Add(new CartItem { ProductId = id, Quantity = quantity });
            }

            // 3. Save back to session
            Session["Cart"] = cart;

            return Json(new { success = true, newCount = cart.Sum(x => x.Quantity) });
        }

        [HttpGet]
        public JsonResult GetCartItems()
        {
            var cart = Session["Cart"] as List<CartItem> ?? new List<CartItem>();

            if (cart.Count == 0)
                return Json(new { success = true, data = new List<object>(), subtotal = 0 }, JsonRequestBehavior.AllowGet);

            // Assuming you have a ProductDAL or DbContext
            // We fetch details for only the IDs present in the session
            var productIds = cart.Select(x => x.ProductId).ToList();
            var products = productDal.GetProductsByIds(productIds); // Replace with your actual data fetch logic

            var result = cart.Select(item => {
                var p = products.FirstOrDefault(x => x.Id == item.ProductId);
                // Use DiscountPrice if OnSale is true, otherwise use normal Price
                decimal unitPrice = (p != null && p.OnSale && p.DiscountPrice > 0)
                                    ? p.DiscountPrice.Value
                                    : (p?.Price ?? 0);

                return new
                {
                    ProductId = item.ProductId,
                    Name = p?.Name ?? "Unknown",
                    Price = unitPrice, // This is the final price (discounted if on sale)
                    OriginalPrice = p?.Price ?? 0,
                    IsOnSale = p?.OnSale ?? false,
                    ActualStockQuantity = p?.StockQuantity,// Add this flag
                    Quantity = item.Quantity,
                    ImagePath = p?.firstVarientproductImages?.FirstOrDefault()?.ImagePath ?? "",
                    Total = unitPrice * item.Quantity
                };
            }).ToList();
            decimal subtotal = result.Sum(x => (decimal)x.Total);

            return Json(new { success = true, data = result, subtotal = subtotal }, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public JsonResult UpdateQuantity(long id, int change)
        {
            var cart = Session["Cart"] as List<CartItem>;
            if (cart != null)
            {
                var item = cart.FirstOrDefault(x => x.ProductId == id);
                if (item != null)
                {
                    item.Quantity += change;
                    // Prevent quantity from going below 1
                    if (item.Quantity < 1) item.Quantity = 1;
                }
                Session["Cart"] = cart;
                Session["CartCount"] = cart.Sum(x => x.Quantity);
            }
            return Json(new { success = true });
        }

        [HttpPost]
        public JsonResult RemoveFromCart(long id)
        {
            var cart = Session["Cart"] as List<CartItem>;
            if (cart != null)
            {
                var item = cart.FirstOrDefault(x => x.ProductId == id);
                if (item != null) cart.Remove(item);

                Session["Cart"] = cart;
                Session["CartCount"] = cart.Sum(x => x.Quantity);
            }
            return Json(new { success = true });
        }

        [HttpPost]
        public JsonResult PrepareCheckout()
        {
            // Re-run the logic from GetCartItems to get the latest prices/data
            var cart = Session["Cart"] as List<CartItem> ?? new List<CartItem>();
            var productIds = cart.Select(x => x.ProductId).ToList();
            var products = productDal.GetProductsByIds(productIds);

            var checkoutData = new CheckoutSessionModel();
            checkoutData.Items = cart.Select(item => {
                var p = products.FirstOrDefault(x => x.Id == item.ProductId);
                decimal unitPrice = (p != null && p.OnSale && p.DiscountPrice > 0) ? p.DiscountPrice.Value : (p?.Price ?? 0);
                return new CartDetailViewModel
                {
                    ProductId = item.ProductId,
                    Name = p?.Name,
                    Price = unitPrice,
                    Quantity = item.Quantity,
                    Total = unitPrice * item.Quantity
                };
            }).ToList();

            checkoutData.GrandTotal = checkoutData.Items.Sum(x => x.Total);

            // Save this detailed object to Session
            Session["CheckoutData"] = checkoutData;

            return Json(new { success = true, redirectUrl = Url.Action("Checkout", "LocalHome", new { area = "EndUser"}) });
        }

        [HttpGet]
        public JsonResult GetCheckoutData()
        {
            // 1. Get Pre-calculated Cart Items from Session
            var checkoutData = Session["CheckoutData"] as CheckoutSessionModel;

            // 2. Check for Logged-in User via Cookie
            var userCookie = Request.Cookies["CustomerAuth"];
            object userData = null;

            if (userCookie != null)
            {
                // In a real app, you'd fetch the full profile from DAL using the ID
                // For now, we take what we have in the cookie
                userData = new
                {
                    UserId = userCookie["UserId"],
                    FullName = userCookie["FullName"],
                    Email = userCookie["Email"]
                };
            }

            return Json(new
            {
                success = true,
                cart = checkoutData,
                user = userData
            }, JsonRequestBehavior.AllowGet);
        }
    }
}