using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace BizOne.Areas.EndUser.Controllers
{
    public class CartController : Controller
    {
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

        // Simple class to hold cart data
        public class CartItem
        {
            public long ProductId { get; set; }
            public int Quantity { get; set; }
        }
    }
}