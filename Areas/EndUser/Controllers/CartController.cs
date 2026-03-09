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
            var cart = Session["Cart"] as CartDetailViewModel ?? new CartDetailViewModel();
            if (cart.Items == null)
            {
                cart.Items = new List<CartItems>();
            }
            // 2. Check if product already exists in cart
            var existingItem = cart.Items.FirstOrDefault(x => x.ProductId == id && x.VariantId == VariantId);
            if (existingItem != null)
            {
                existingItem.Quantity += quantity;
            }
            else
            {
                cart.Items.Add(new CartItems { ProductId = id, Quantity = quantity, VariantId = VariantId });
            }

            // 3. Save back to session
            Session["Cart"] = cart;

            return Json(new { success = true, newCount = cart.Items.Sum(x => x.Quantity) });
        }

        [HttpGet]
        public JsonResult GetCartItems(string code)
        {
            // 1. Get Currency Info from Cookies
            string currency = Request.Cookies["SelectedCurrency"]?.Value ?? "PKR";
            string symbol = Request.Cookies["SelectedSymbol"]?.Value ?? "Rs.";
            string rateStr = Request.Cookies["SelectedRate"]?.Value ?? "1";
            decimal exchangeRate = decimal.TryParse(rateStr, out decimal r) ? r : 1.0m;

            var cart = Session["Cart"] as CartDetailViewModel ?? new CartDetailViewModel(); ;

            if (cart.Items.Count == 0)
                return Json(new { success = true, data = new List<object>(), subtotal = 0, symbol = symbol }, JsonRequestBehavior.AllowGet);

            bool hasCoupon = false;
            CouponModel coupendata = null;
            if (!string.IsNullOrEmpty(code))
            {
                coupendata = productDal.GetCouponByIdorCode(null, 7, code);
            }
            else if (cart.HasCoupon)
            {
                coupendata = cart.CoupenData;
            }

            if (coupendata != null)
            {
                hasCoupon = true;
            }
            
            foreach (var item in cart.Items)
            {
                var fullProduct = productDal.GetProductDetailsById(item.ProductId);
                if (fullProduct == null) continue;

                item.Name = fullProduct.Name;
                string categoryTree = string.Join(" > ", fullProduct.categoriesorder.Select(c => c.CategoryName));
                item.CategoryTree = categoryTree;
                if (item.VariantId.HasValue && item.VariantId.Value > 0)
                {
                    var v = fullProduct.varients.FirstOrDefault(x => x.Id == item.VariantId.Value);
                    if (v != null)
                    {
                        var details = new List<string>();
                        if (!string.IsNullOrWhiteSpace(fullProduct.Colour)) details.Add($"Color: {fullProduct.Colour}");
                        if (!string.IsNullOrWhiteSpace(fullProduct.Size.ToString())) details.Add($"Size: {fullProduct.Size}");
                        if (!string.IsNullOrWhiteSpace(fullProduct.Material)) details.Add($"Material: {fullProduct.Material}");
                        item.VariantInfo = details.Any() ? string.Join(", ", details) : null;
                        var vImg = v.imageslist.FirstOrDefault(i => i.IsPrimary) ?? v.imageslist.FirstOrDefault();
                        item.ImgPath = vImg?.ImagePath ?? "";
                        item.ActualPrice = v.Price ?? 0;
                        item.OnSale = v.OnSale;
                        if (v.OnSale && v.DiscountRate > 0)
                        {
                            item.DiscountRate = v.DiscountRate;
                            item.DiscountPrice = v.Price * (v.DiscountRate / 100m);
                            item.PriceafterApplysalediscount = v.Price - (item.DiscountPrice);
                        }
                        else if (v.OnSale && v.DiscountPrice > 0)
                        {
                            item.DiscountPrice = v.DiscountPrice;
                            item.DiscountRate = (v.DiscountPrice / v.Price) * 100;
                            item.PriceafterApplysalediscount = v.Price - v.DiscountPrice;
                        }

                        item.StockQty = v.StockQuantity;
                        item.Final1UnitPrice = v.OnSale ? item.PriceafterApplysalediscount : item.ActualPrice;
                        

                    }
                }
                else
                {
                    var details = new List<string>();
                    if (!string.IsNullOrWhiteSpace(fullProduct.Colour)) details.Add($"Color: {fullProduct.Colour}");
                    if (!string.IsNullOrWhiteSpace(fullProduct.Size.ToString())) details.Add($"Size: {fullProduct.Size}");
                    if (!string.IsNullOrWhiteSpace(fullProduct.Material)) details.Add($"Material: {fullProduct.Material}");
                    item.VariantInfo = details.Any() ? string.Join(", ", details) : null;
                    var pImg = fullProduct.ProductImages.FirstOrDefault(i => i.IsPrimary) ?? fullProduct.ProductImages.FirstOrDefault();
                    item.ImgPath = pImg?.ImagePath ?? "";
                    item.ActualPrice = fullProduct.Price ?? 0;
                    item.OnSale = fullProduct.OnSale;
                    if (fullProduct.OnSale && fullProduct.DiscountRate > 0)
                    {
                        item.DiscountRate = fullProduct.DiscountRate;
                        item.DiscountPrice = fullProduct.Price * (fullProduct.DiscountRate / 100m);
                        item.PriceafterApplysalediscount = fullProduct.Price - (item.DiscountPrice);
                    }
                    else if (fullProduct.OnSale && fullProduct.DiscountPrice > 0)
                    {
                        item.DiscountPrice = fullProduct.DiscountPrice;
                        item.DiscountRate = (fullProduct.DiscountPrice / fullProduct.Price) * 100;
                        item.PriceafterApplysalediscount = fullProduct.Price - fullProduct.DiscountPrice;
                    }
                    
                    item.Final1UnitPrice = fullProduct.OnSale ? item.PriceafterApplysalediscount : item.ActualPrice;
                    item.StockQty = fullProduct.StockQuantity;
                    
                }

                
                item.Total1UnitPrice = item.Quantity * item.Final1UnitPrice;
                item.ConvertedUnitPrice = (decimal?)Math.Round((double)(item.Total1UnitPrice / exchangeRate), 2);

            }

            // 3. Apply Coupon Types Logic
            decimal orderDiscount = 0;
            if (hasCoupon)
            {
                decimal couponRate = coupendata.DiscountRate ?? 0;
                decimal couponFixed = coupendata.DiscountPrice ?? 0;

                switch (coupendata.CouponType)
                {
                    case "Discount": // Percentage or Fixed on Full Order
                        decimal currentSub = cart.Items.Sum(x => x.Quantity * (x.Final1UnitPrice ?? 0));
                        orderDiscount = (couponRate > 0) ? (currentSub * (couponRate / 100)) : couponFixed;
                        break;

                    case "DiscountWithoutSaleItems":
                        foreach (var item in cart.Items.Where(i => !i.OnSale))
                        {
                            decimal itemTotal = item.Quantity * (item.Final1UnitPrice ?? 0);
                            item.DiscountPricebyCoupon = (couponRate > 0) ? (itemTotal * (couponRate / 100)) : couponFixed;
                            orderDiscount += item.DiscountPricebyCoupon ?? 0;
                        }
                        break;

                    case "Discount1stItem":
                        foreach (var item in cart.Items)
                        {
                            decimal discountOnOne = (couponRate > 0) ? ((item.Final1UnitPrice ?? 0) * (couponRate / 100)) : couponFixed;
                            item.DiscountPricebyCoupon = discountOnOne; // Only applies once per unique product line
                            orderDiscount += discountOnOne;
                        }
                        break;

                    case "BOGO": // Buy 1 Get 1 (100% off the second item)
                        foreach (var item in cart.Items.Where(i => i.Quantity >= 2))
                        {
                            int freeQty = item.Quantity / 2;
                            orderDiscount += freeQty * (item.Final1UnitPrice ?? 0);
                        }
                        break;

                    case "BTGO": // Buy 2 Get 1
                        foreach (var item in cart.Items.Where(i => i.Quantity >= 3))
                        {
                            int freeQty = item.Quantity / 3;
                            orderDiscount += freeQty * (item.Final1UnitPrice ?? 0);
                        }
                        break;

                    case "BTGT": // Buy 2 Get 2
                        foreach (var item in cart.Items.Where(i => i.Quantity >= 4))
                        {
                            int freeQty = (item.Quantity / 4) * 2;
                            orderDiscount += freeQty * (item.Final1UnitPrice ?? 0);
                        }
                        break;

                    case "Free Shipping":
                        // Handled at Grand Total level (Shipping = 0)
                        break;
                }
            }

            // 4. Calculate Final Totals & Converted Values
            foreach (var item in cart.Items)
            {
                item.Total1UnitPrice = item.Quantity * item.Final1UnitPrice;
                // Subtotal after internal item-level adjustments if any
                item.ConvertedUnitPrice = (decimal?)Math.Round((double)(item.Total1UnitPrice / exchangeRate), 2);
            }

            decimal rawSubtotal = cart.Items.Sum(x => x.Quantity * (x.Final1UnitPrice ?? 0));
            decimal finalSubtotal = (rawSubtotal - orderDiscount) / exchangeRate;

            CartDetailViewModel cartData = new CartDetailViewModel
            {
                Items = cart.Items,
                Subtotal = (decimal?)Math.Round(finalSubtotal, 2),
                HasCoupon = hasCoupon,
                CoupenCode = coupendata?.CouponCode,
                CoupenData = coupendata
            };

            Session["Cart"] = cartData;
            // Inside GetCartItems after calculating everything:
            return Json(new
            {
                success = true,
                data = cartData,
                subtotal = cartData.Subtotal, // This is the final total
                rawSubtotal = Math.Round(rawSubtotal / exchangeRate, 2), // The price before coupon
                discount = Math.Round(orderDiscount / exchangeRate, 2), // The coupon savings
                symbol = symbol
            }, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public JsonResult UpdateQuantity(long id,long? VariantId, int change)
        {
            var cart = Session["Cart"] as CartDetailViewModel;
            if (cart != null)
            {
                var item = cart.Items.FirstOrDefault(x => x.ProductId == id && x.VariantId == VariantId);
                if (item != null)
                {
                    item.Quantity += change;
                    // Prevent quantity from going below 1
                    if (item.Quantity < 1) item.Quantity = 1;
                }
                Session["Cart"] = cart;
                Session["CartCount"] = cart.Items.Sum(x => x.Quantity);
            }
            return Json(new { success = true });
        }

        [HttpPost]
        public JsonResult RemoveFromCart(long id, long? VariantId)
        {
            var cart = Session["Cart"] as CartDetailViewModel;
            if (cart != null)
            {
                var item = cart.Items.FirstOrDefault(x => x.ProductId == id && x.VariantId == VariantId);
                if (item != null) cart.Items.Remove(item);
                Session["Cart"] = cart;
                Session["CartCount"] = cart.Items.Sum(x => x.Quantity);
            }
            return Json(new { success = true });
        }

        [HttpGet]
        public JsonResult VarifyCouponCode(string code)
        {
            // 1. Fetch Coupon Data
            CouponModel coupon = productDal.GetCouponByIdorCode(null, 7, code);

            if (coupon == null || coupon.Id == 0)
            {
                return Json(new { success = false, message = "Invalid coupon code." }, JsonRequestBehavior.AllowGet);
            }

            // 2. Check if Active
            if (!coupon.IsActive)
            {
                return Json(new { success = false, message = "This coupon is no longer active." }, JsonRequestBehavior.AllowGet);
            }

            // 3. Check Expiry Date
            if (!string.IsNullOrEmpty(coupon.ExpiryDate))
            {
                DateTime expiry = DateTime.Parse(coupon.ExpiryDate);
                if (DateTime.Now.Date > expiry.Date)
                {
                    return Json(new { success = false, message = "This coupon has expired." }, JsonRequestBehavior.AllowGet);
                }
            }

            // 4. Check Usage Limit
            if (coupon.UsageLimit <= 0) // Assuming usage limit decrements or is compared against a count
            {
                return Json(new { success = false, message = "Usage limit reached for this coupon." }, JsonRequestBehavior.AllowGet);
            }

            // 5. Check Customer Ownership (if Purpose is 'Specific')
            if (coupon.Purpose == "Specific")
            {
                var userCookie = Request.Cookies["CustomerAuth"];
                if (userCookie == null)
                {
                    return Json(new { success = false, message = "Please login to use this specific coupon." }, JsonRequestBehavior.AllowGet);
                }

                long loggedInUserId = Convert.ToInt64(userCookie["UserId"]);
                if (coupon.CustomerId != loggedInUserId)
                {
                    return Json(new { success = false, message = "This coupon is not valid for your account." }, JsonRequestBehavior.AllowGet);
                }
            }

            CouponModel showCoupenData = new CouponModel();
            showCoupenData.Purpose = coupon.Purpose;
            showCoupenData.CouponType = coupon.CouponType;
            showCoupenData.DiscountRate = coupon.DiscountRate;
            showCoupenData.DiscountPrice = coupon.DiscountPrice;
            showCoupenData.CouponCode = coupon.CouponCode;

            // All checks passed
            return Json(new
            {
                success = true,
                message = $"Coupon available!",
                CoupenData = showCoupenData
            }, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public JsonResult RemoveCoupon()
        {
            var cart = Session["Cart"] as CartDetailViewModel;
            if (cart != null)
            {
                cart.HasCoupon = false;
                cart.CoupenCode = null;
                cart.CoupenData = null;

                // Clear item-level coupon discounts
                foreach (var item in cart.Items)
                {
                    item.DiscountPricebyCoupon = 0;
                }

                Session["Cart"] = cart;
            }
            return Json(new { success = true });
        }
    }
}