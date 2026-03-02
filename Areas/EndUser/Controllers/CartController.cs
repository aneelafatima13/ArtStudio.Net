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
        public JsonResult AddToCart(long id, int quantity, long VariantId)
        {
            // 1. Get current cart from session or create new one
            var cart = Session["Cart"] as List<CartItem> ?? new List<CartItem>();

            // 2. Check if product already exists in cart
            var existingItem = cart.FirstOrDefault(x => x.ProductId == id || x.VariantId == VariantId);
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
            var cart = Session["Cart"] as List<CartItem> ?? new List<CartItem>();

            if (cart.Count == 0)
                return Json(new { success = true, data = new List<object>(), subtotal = 0 }, JsonRequestBehavior.AllowGet);

            var resultList = new List<object>();

            foreach (var item in cart)
            {
                // 1. Fetch the full product object (including all its variants and images)
                var fullProduct = productDal.GetProductDetailsById(item.ProductId);
                if (fullProduct == null) continue;

                decimal unitPrice = 0;
                string displayName = fullProduct.Name;
                string imagePath = "";
                long? stockQty = 0;
                string variantInfo = "";

                // 2. Determine if we use Variant data or Base Product data
                if (item.VariantId.HasValue && item.VariantId.Value > 0)
                {
                    // Find the specific variant in the list
                    var v = fullProduct.varients.FirstOrDefault(x => x.Id == item.VariantId.Value);
                    if (v != null)
                    {
                        // Use Variant Price logic
                        unitPrice = (v.OnSale && v.DiscountPrice > 0) ? v.DiscountPrice.Value : (v.Price ?? 0);
                        stockQty = v.StockQuantity;
                        variantInfo = $"{v.Colour} - {v.Size}";

                        // Get Variant Primary Image or first available variant image
                        var vImg = v.imageslist.FirstOrDefault(i => i.IsPrimary) ?? v.imageslist.FirstOrDefault();
                        imagePath = vImg?.ImagePath ?? "";
                    }
                }
                else
                {
                    // Use Base Product Price logic
                    unitPrice = (fullProduct.OnSale && fullProduct.DiscountPrice > 0) ? fullProduct.DiscountPrice.Value : (fullProduct.Price ?? 0);
                    stockQty = fullProduct.StockQuantity;

                    // Get Base Product Primary Image
                    var pImg = fullProduct.ProductImages.FirstOrDefault(i => i.IsPrimary) ?? fullProduct.ProductImages.FirstOrDefault();
                    imagePath = pImg?.ImagePath ?? "";
                }

                // 3. Build the individual order object
                resultList.Add(new
                {
                    ProductId = item.ProductId,
                    VariantId = item.VariantId,
                    Name = displayName,
                    VariantDetails = variantInfo, // Extra info for the UI (e.g., "Red - XL")
                    Price = unitPrice,
                    ActualStockQuantity = stockQty,
                    Quantity = item.Quantity,
                    ImagePath = imagePath,
                    Total = unitPrice * item.Quantity
                });
            }

            decimal subtotal = resultList.Sum(x => (decimal)((dynamic)x).Total);

            return Json(new { success = true, data = resultList, subtotal = subtotal }, JsonRequestBehavior.AllowGet);
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
            var cart = Session["Cart"] as List<CartItem> ?? new List<CartItem>();

            if (cart.Count == 0)
            {
                return Json(new { success = false, message = "Your cart is empty." });
            }

            var checkoutData = new CheckoutSessionModel();
            checkoutData.Items = new List<CartDetailViewModel>();

            foreach (var item in cart)
            {
                // Fetch full product details (includes base product + variants)
                var fullProduct = productDal.GetProductDetailsById(item.ProductId);
                if (fullProduct == null) continue;

                decimal unitPrice = 0;
                string displayName = fullProduct.Name;
                string variantDescription = "";

                // Check if this cart item is a specific variant
                if (item.VariantId.HasValue && item.VariantId.Value > 0)
                {
                    var v = fullProduct.varients.FirstOrDefault(x => x.Id == item.VariantId.Value);
                    if (v != null)
                    {
                        // Use Variant Price logic
                        unitPrice = (v.OnSale && v.DiscountPrice > 0) ? v.DiscountPrice.Value : (v.Price ?? 0);
                        variantDescription = $"({v.Colour} - {v.Size})";
                    }
                }
                else
                {
                    // Use Base Product Price logic
                    unitPrice = (fullProduct.OnSale && fullProduct.DiscountPrice > 0) ? fullProduct.DiscountPrice.Value : (fullProduct.Price ?? 0);
                }

                checkoutData.Items.Add(new CartDetailViewModel
                {
                    ProductId = item.ProductId,
                    VariantId = item.VariantId, // Ensure your ViewModel has this property
                    Name = $"{displayName} {variantDescription}".Trim(),
                    Price = unitPrice,
                    Quantity = item.Quantity,
                    Total = unitPrice * item.Quantity
                });
            }

            // Calculate Final Total
            checkoutData.GrandTotal = checkoutData.Items.Sum(x => x.Total);

            // Save the fully populated object to Session for the Checkout Page to read
            Session["CheckoutData"] = checkoutData;

            return Json(new
            {
                success = true,
                redirectUrl = Url.Action("Checkout", "LocalHome", new { area = "EndUser" })
            });
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