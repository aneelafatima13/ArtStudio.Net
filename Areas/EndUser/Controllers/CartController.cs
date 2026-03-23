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

       public JsonResult GetCoupens()
        {
            List<CouponModel> coupons = productDal.GetCoupons();
            return Json(new { success = true, data = coupons }, JsonRequestBehavior.AllowGet);
        }

        [HttpGet]
        public JsonResult GetCartItems(string code)
        {
            // 1. Get Currency Info
            string symbol = Request.Cookies["SelectedSymbol"]?.Value ?? "Rs.";
            string rateStr = Request.Cookies["SelectedRate"]?.Value ?? "1";
            decimal exchangeRate = decimal.TryParse(rateStr, out decimal r) ? r : 1.0m;

            var cart = Session["Cart"] as CartDetailViewModel ?? new CartDetailViewModel();

            if (cart.Items == null || cart.Items.Count == 0)
                return Json(new { success = true, data = new List<object>(), subtotal = 0, symbol = symbol }, JsonRequestBehavior.AllowGet);

            // 2. Add New Coupon if provided
            if (!string.IsNullOrEmpty(code))
            {
                // Check if coupon is already applied to prevent duplicates
                if (!cart.AppliedCoupons.Any(c => c.CouponCode.Equals(code, StringComparison.OrdinalIgnoreCase)))
                {
                    var newCoupon = productDal.GetCouponByIdorCode(null, 7, code);
                    if (newCoupon != null)
                    {
                        cart.AppliedCoupons.Add(newCoupon);
                    }
                }
            }

            // 3. Refresh Product Data (Price/Sale/Stock)
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


                
                // If your Model has these properties, convert them for the UI:
                item.ActualPrice = Math.Round(item.ActualPrice / exchangeRate, 2);
                if (item.DiscountPrice.HasValue)
                    item.DiscountPrice = Math.Round(item.DiscountPrice.Value / exchangeRate, 2);
                if (item.PriceafterApplysalediscount.HasValue)
                    item.PriceafterApplysalediscount = Math.Round(item.PriceafterApplysalediscount.Value / exchangeRate, 2);
                if (item.Final1UnitPrice.HasValue)
                    item.Final1UnitPrice = Math.Round(item.Final1UnitPrice.Value / exchangeRate, 2);
                item.Total1UnitPrice = item.Quantity * item.Final1UnitPrice;
            }

            // 4. Calculate Coupon Discounts (Loop through all applied coupons)
            decimal totalOrderDiscount = 0;
            bool hasFreeShipping = false;
            decimal currentRawSubtotal = cart.Items.Sum(x => x.Total1UnitPrice ?? 0);



            foreach (var coupon in cart.AppliedCoupons)
            {
                decimal couponRate = coupon.DiscountRate ?? 0;
                decimal couponFixed = coupon.DiscountPrice ?? 0;

                switch (coupon.CouponType)
                {
                    case "Discount":
                        totalOrderDiscount += (couponRate > 0) ? (currentRawSubtotal * (couponRate / 100)) : couponFixed;
                        
                        break;

                    case "DiscountWithoutSaleItems":
                        foreach (var item in cart.Items.Where(i => !i.OnSale))
                        {
                            totalOrderDiscount += (couponRate > 0) ? ((item.Total1UnitPrice ?? 0) * (couponRate / 100)) : couponFixed;
                        }
                        break;

                    case "BOGO":
                        foreach (var item in cart.Items.Where(i => i.Quantity >= 2))
                        {
                            totalOrderDiscount += (item.Quantity / 2) * (item.Final1UnitPrice ?? 0);
                        }
                        break;
                    case "BTGO":
                        foreach (var item in cart.Items.Where(i => i.Quantity >= 3))
                        {
                            totalOrderDiscount += (item.Quantity / 2) * (item.Final1UnitPrice ?? 0);
                        }
                        break;
                   case "BTGT":
                        foreach (var item in cart.Items.Where(i => i.Quantity >= 4))
                        {
                            totalOrderDiscount += (item.Quantity / 2) * (item.Final1UnitPrice ?? 0);
                        }
                        break;

                    case "Free Shipping":
                        hasFreeShipping = true;
                        break;

                    case "FreeShippingMinOrder":
                        if (currentRawSubtotal >= coupon.minorderprice)
                            hasFreeShipping = true;
                        break;

                        // Add other cases (BTGO, Discount1stItem) similarly...
                }

                totalOrderDiscount = Math.Round(totalOrderDiscount / exchangeRate, 2);
            }

            // 5. Final Totals & Currency Conversion
            decimal baseShipping = hasFreeShipping ? 0 : 200;

            // Convert to selected currency
            decimal convertedSubtotal = (currentRawSubtotal - totalOrderDiscount);
            decimal convertedShipping = baseShipping / exchangeRate;
            decimal convertedGrandTotal = convertedSubtotal + convertedShipping;

            // 6. Update Session Object
            cart.Subtotal = Math.Round(convertedSubtotal, 2);
            cart.ShippingAmount = Math.Round(convertedShipping, 2);
            cart.GrandTotal = Math.Round(convertedGrandTotal, 2);
            cart.TotalCouponDiscount = totalOrderDiscount;
            cart.HasFreeShipping = hasFreeShipping;
            

            Session["Cart"] = cart;

            return Json(new
            {
                success = true,
                data = cart,
                appliedCodes = cart.AppliedCoupons.Select(c => c.CouponCode).ToList(),
                subtotal = cart.Subtotal,
                rawSubtotal = Math.Round(currentRawSubtotal / exchangeRate, 2),
                discount = cart.TotalCouponDiscount,
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
        public JsonResult VerifyCouponCode(string code)
        {
            // Standardize input to handle case-sensitivity and whitespace
            code = code?.Trim().ToUpper();

            CouponModel coupon = productDal.GetCouponByIdorCode(null, 7, code);
            var cart = Session["Cart"] as CartDetailViewModel;

            if (coupon == null || coupon.Id == 0)
            {
                return Json(new { success = false, message = "Invalid coupon code." }, JsonRequestBehavior.AllowGet);
            }

            if (!coupon.IsActive)
            {
                return Json(new { success = false, message = "This coupon is no longer active." }, JsonRequestBehavior.AllowGet);
            }

            // 4. Expiry Validation
            if (!string.IsNullOrEmpty(coupon.ExpiryDate))
            {
                if (DateTime.TryParse(coupon.ExpiryDate, out DateTime expiry))
                {
                    if (DateTime.Now.Date > expiry.Date)
                        return Json(new { success = false, message = "This coupon has expired." }, JsonRequestBehavior.AllowGet);
                }
            }

            // 5. Usage Limit Validation
            if (coupon.UsageLimit <= coupon.UsedNos || coupon.UsageLimit != -1)
            {
                return Json(new { success = false, message = "Usage limit reached for this coupon." }, JsonRequestBehavior.AllowGet);
            }

            // 6. Specific Customer Validation
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

            // 7. Cart Context Validation
            if (cart == null || !cart.Items.Any())
            {
                return Json(new { success = false, message = "Your cart is empty." }, JsonRequestBehavior.AllowGet);
            }

            decimal currentSubtotal = cart.Items.Sum(x => x.Quantity * (x.Final1UnitPrice ?? 0));
            decimal calculatedDiscount = 0;
            string successMsg = "Coupon applied!";

            // 8. Logic for Advanced Coupon Types
            switch (coupon.CouponType)
            {
                case "Discount":
                case "Cashback":
                case "FirstPurchase":
                    calculatedDiscount = (currentSubtotal * (coupon.DiscountRate ?? 0)) / 100;
                    break;

                case "DiscountWithoutSaleItems":
                    decimal eligibleSubtotal = cart.Items.Where(i => !i.OnSale).Sum(i => i.Quantity * (i.Final1UnitPrice ?? 0));
                    if (eligibleSubtotal <= 0)
                        return Json(new { success = false, message = "Coupon only applies to non-sale items." }, JsonRequestBehavior.AllowGet);

                    calculatedDiscount = (eligibleSubtotal * (coupon.DiscountRate ?? 0)) / 100;
                    break;

                case "Discount1stItem":
                    // Applies discount only to the first item in the list
                    var firstItem = cart.Items.FirstOrDefault();
                    if (firstItem != null)
                        calculatedDiscount = (firstItem.Final1UnitPrice ?? 0) * (coupon.DiscountRate ?? 0) / 100;
                    break;

                case "BOGO": // Buy 1 Get 1 (Free)
                    var bogoItem = cart.Items.FirstOrDefault(i => i.Quantity >= 2);
                    if (bogoItem == null)
                        return Json(new { success = false, message = "Add at least 2 of the same item for BOGO." }, JsonRequestBehavior.AllowGet);

                    calculatedDiscount = (bogoItem.Final1UnitPrice ?? 0);
                    break;

                case "BTGO": // Buy 2 Get 1 (Free)
                    var btgoItem = cart.Items.FirstOrDefault(i => i.Quantity >= 3);
                    if (btgoItem == null)
                        return Json(new { success = false, message = "Add 3 of the same item for Buy 2 Get 1." }, JsonRequestBehavior.AllowGet);

                    calculatedDiscount = (btgoItem.Final1UnitPrice ?? 0);
                    break;

                case "BTGT": // Buy 2 Get 2 (Free)
                    var btgtItem = cart.Items.FirstOrDefault(i => i.Quantity >= 4);
                    if (btgtItem == null)
                        return Json(new { success = false, message = "Add 4 of the same item for Buy 2 Get 2." }, JsonRequestBehavior.AllowGet);

                    calculatedDiscount = (btgtItem.Final1UnitPrice ?? 0) * 2;
                    break;

                case "Free Shipping":
                case "FreeShippingMinOrder":
                    if (coupon.CouponType == "FreeShippingMinOrder" && currentSubtotal < (coupon.minorderprice ?? 0))
                        return Json(new { success = false, message = $"Spend {coupon.minorderprice} for free shipping." }, JsonRequestBehavior.AllowGet);

                    successMsg = "Free shipping applied!";
                    calculatedDiscount = 0; // Usually handled by making Shipping = 0 in the total calculation
                    break;

                default:
                    calculatedDiscount = 0;
                    break;
            }
            // 9. Apply Max Discount Cap (DiscountPrice)
            // Example: 10% discount but capped at 500 PKR
            if (coupon.DiscountPrice > 0 && calculatedDiscount > coupon.DiscountPrice)
            {
                calculatedDiscount = coupon.DiscountPrice ?? 0;
            }

            // 10. Prepare final data for Frontend
            var returnData = new
            {
                coupon.CouponCode,
                coupon.CouponType,
                coupon.DiscountRate,
                MaxDiscountCap = coupon.DiscountPrice,
                DiscountValue = calculatedDiscount // The actual money saved
            };

            return Json(new
            {
                success = true,
                message = successMsg,
                CoupenData = returnData
            }, JsonRequestBehavior.AllowGet);
        }



        [HttpPost]
        public JsonResult RemoveCoupon(string couponCode)
        {
            var cart = Session["Cart"] as CartDetailViewModel;
            if (cart != null && !string.IsNullOrEmpty(couponCode))
            {
                // Remove the specific coupon from the list
                var couponToRemove = cart.AppliedCoupons
                    .FirstOrDefault(c => c.CouponCode.Equals(couponCode, StringComparison.OrdinalIgnoreCase));

                if (couponToRemove != null)
                {
                    cart.AppliedCoupons.Remove(couponToRemove);

                    // Reset item-level coupon discounts so they can be recalculated
                    foreach (var item in cart.Items)
                    {
                        item.DiscountPricebyCoupon = 0;
                    }

                    Session["Cart"] = cart;
                    return Json(new { success = true, message = "Coupon removed successfully" });
                }
            }
            return Json(new { success = false, message = "Coupon not found" });
        }

    }
}