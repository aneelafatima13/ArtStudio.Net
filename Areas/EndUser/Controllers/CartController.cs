using BizOne.Common;
using BizOne.DAL;
using Microsoft.Ajax.Utilities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace BizOne.Areas.EndUser.Controllers
{
    public class CartController : Controller
    {
        private readonly ProductsDAL productDal = new ProductsDAL();

        public ActionResult Cart()
        {
            return View();
        }
        [HttpPost]
        public JsonResult AddToCart(long id, int quantity, long? VariantId)
        {
            // 1. Get current cart from session or create new one
            var cart = Session["Cart"] as List<CartItem> ?? new List<CartItem>();

            // 2. Check if product already exists in cart
            var existingItem = cart.FirstOrDefault(x => x.ProductId == id && x.VariantId == VariantId);
            if (existingItem != null)
            {
                existingItem.Quantity += quantity;
            }
            else
            {
                cart.Add(new CartItem { ProductId = id, Quantity = quantity, VariantId = VariantId });
            }

            // 3. Save back to session
            Session["Cart"] = cart;

            return Json(new { success = true, newCount = cart.Sum(x => x.Quantity) });
        }

        [HttpGet]
        public JsonResult GetCartItems()
        {
            // 1. Get Currency Info from Cookies
            string currency = Request.Cookies["SelectedCurrency"]?.Value ?? "PKR";
            string symbol = Request.Cookies["SelectedSymbol"]?.Value ?? "Rs.";
            string rateStr = Request.Cookies["SelectedRate"]?.Value ?? "1";
            decimal exchangeRate = decimal.TryParse(rateStr, out decimal r) ? r : 1.0m;

            var cart = Session["Cart"] as List<CartItem> ?? new List<CartItem>();

            if (cart.Count == 0)
                return Json(new { success = true, data = new List<object>(), subtotal = 0, symbol = symbol }, JsonRequestBehavior.AllowGet);

            var resultList = new List<object>();

            foreach (var item in cart)
            {
                var fullProduct = productDal.GetProductDetailsById(item.ProductId);
                if (fullProduct == null) continue;

                decimal pkrPrice = 0;
                string displayName = fullProduct.Name;
                string imagePath = "";
                long? stockQty = 0;
                string variantInfo = "";

                if (item.VariantId.HasValue && item.VariantId.Value > 0)
                {
                    var v = fullProduct.varients.FirstOrDefault(x => x.Id == item.VariantId.Value);
                    if (v != null)
                    {
                        pkrPrice = (v.OnSale && v.DiscountPrice > 0) ? v.DiscountPrice.Value : (v.Price ?? 0);
                        stockQty = v.StockQuantity;
                        variantInfo = $"{v.Colour} - {v.Size}";
                        var vImg = v.imageslist.FirstOrDefault(i => i.IsPrimary) ?? v.imageslist.FirstOrDefault();
                        imagePath = vImg?.ImagePath ?? "";
                    }
                }
                else
                {
                    pkrPrice = (fullProduct.OnSale && fullProduct.DiscountPrice > 0) ? fullProduct.DiscountPrice.Value : (fullProduct.Price ?? 0);
                    stockQty = fullProduct.StockQuantity;
                    var pImg = fullProduct.ProductImages.FirstOrDefault(i => i.IsPrimary) ?? fullProduct.ProductImages.FirstOrDefault();
                    imagePath = pImg?.ImagePath ?? "";
                }

                // 2. Convert Price to Selected Currency
                decimal convertedUnitPrice = Math.Round(pkrPrice / exchangeRate, 2);

                resultList.Add(new
                {
                    ProductId = item.ProductId,
                    VariantId = item.VariantId,
                    Name = displayName,
                    VariantDetails = variantInfo,
                    Price = convertedUnitPrice, // Converted
                    ActualStockQuantity = stockQty,
                    Quantity = item.Quantity,
                    ImagePath = imagePath,
                    Total = convertedUnitPrice * item.Quantity // Converted Total
                });
            }

            decimal subtotal = resultList.Sum(x => (decimal)((dynamic)x).Total);

            return Json(new
            {
                success = true,
                data = resultList,
                subtotal = subtotal,
                symbol = symbol // Send symbol to UI
            }, JsonRequestBehavior.AllowGet);
        }
        [HttpPost]
        public JsonResult UpdateQuantity(long id,long? VariantId, int change)
        {
            var cart = Session["Cart"] as List<CartItem>;
            if (cart != null)
            {
                var item = cart.FirstOrDefault(x => x.ProductId == id && x.VariantId == VariantId);
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
        public JsonResult RemoveFromCart(long id, long? VariantId)
        {
            var cart = Session["Cart"] as List<CartItem>;
            if (cart != null)
            {
                var item = cart.FirstOrDefault(x => x.ProductId == id && x.VariantId == VariantId);
                if (item != null) cart.Remove(item);
                Session["Cart"] = cart;
                Session["CartCount"] = cart.Sum(x => x.Quantity);
            }
            return Json(new { success = true });
        }

        
        
    }
}