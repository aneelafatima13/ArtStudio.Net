using BizOne.Common;
using BizOne.DAL;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace BizOne.Areas.EndUser.Controllers
{
    public class CheckoutController : Controller
    {
        private readonly ProductsDAL productDal = new ProductsDAL();

        // GET: EndUser/Checkout
        public ActionResult Checkout()
        {
            return View();
        }

        [HttpPost]
        public JsonResult PrepareCheckout()
        {
            string currency = Request.Cookies["SelectedCurrency"]?.Value ?? "PKR";
            string symbol = Request.Cookies["SelectedSymbol"]?.Value ?? "Rs.";
            string rateStr = Request.Cookies["SelectedRate"]?.Value ?? "1";
            decimal exchangeRate = decimal.TryParse(rateStr, out decimal r) ? r : 1.0m;
            var cart = Session["Cart"] as List<CartItem> ?? new List<CartItem>();
            if (cart.Count == 0) return Json(new { success = false, message = "Your cart is empty." });

            var checkoutData = new CheckoutSessionModel();
            checkoutData.Items = new List<CartDetailViewModel>();

            foreach (var item in cart)
            {
                decimal pkrUnitPrice = 0;
                decimal pkrActualPrice = 0;

                var fullProduct = productDal.GetProductDetailsById(item.ProductId);
                if (fullProduct == null) continue;

                decimal unitPrice = 0;
                string displayName = fullProduct.Name;
                string itemDetails = "";
                string sourceImgPath = "";
                bool onSale = false;
                decimal discountRate = 0;
                decimal actualPrice = 0;
                string description = fullProduct.Description;
                // 1. Logic for Variant vs Base Product
                if (item.VariantId.HasValue && item.VariantId.Value > 0)
                {
                    var v = fullProduct.varients.FirstOrDefault(x => x.Id == item.VariantId.Value);
                    if (v != null)
                    {
                        pkrActualPrice = v.Price ?? 0;
                        pkrUnitPrice = (v.OnSale && v.DiscountPrice > 0) ? v.DiscountPrice.Value : (v.Price ?? 0);
                        onSale = v.OnSale;
                        discountRate = v.DiscountRate ?? 0;
                        actualPrice = v.Price ?? 0;
                        unitPrice = (v.OnSale && v.DiscountPrice > 0) ? v.DiscountPrice.Value : (v.Price ?? 0);

                        var details = new List<string>();
                        if (!string.IsNullOrWhiteSpace(v.Colour)) details.Add($"Color: {v.Colour}");
                        if (!string.IsNullOrWhiteSpace(v.Size.ToString())) details.Add($"Size: {v.Size}");
                        if (!string.IsNullOrWhiteSpace(fullProduct.Material)) details.Add($"Material: {fullProduct.Material}");

                        // Join with a comma or keep null/empty if none exist
                        itemDetails = details.Any() ? string.Join(", ", details) : null;
                        sourceImgPath = v.imageslist.OrderByDescending(i => i.IsPrimary).Select(i => i.ImagePath).FirstOrDefault();
                    }
                }
                else
                {
                    pkrActualPrice = fullProduct.Price ?? 0;
                    pkrUnitPrice = (fullProduct.OnSale && fullProduct.DiscountPrice > 0) ? fullProduct.DiscountPrice.Value : (fullProduct.Price ?? 0);
                    onSale = fullProduct.OnSale;
                    discountRate = fullProduct.DiscountRate ?? 0;
                    actualPrice = fullProduct.Price ?? 0;
                    unitPrice = (fullProduct.OnSale && fullProduct.DiscountPrice > 0) ? fullProduct.DiscountPrice.Value : (fullProduct.Price ?? 0);
                    var details = new List<string>();
                    if (!string.IsNullOrWhiteSpace(fullProduct.Colour)) details.Add($"Color: {fullProduct.Colour}");
                    if (!string.IsNullOrWhiteSpace(fullProduct.Size.ToString())) details.Add($"Size: {fullProduct.Size}");
                    if (!string.IsNullOrWhiteSpace(fullProduct.Material)) details.Add($"Material: {fullProduct.Material}");

                    // Join with a comma or keep null/empty if none exist
                    itemDetails = details.Any() ? string.Join(", ", details) : null;
                    // Get Base Product Primary Image
                    sourceImgPath = fullProduct.ProductImages.OrderByDescending(i => i.IsPrimary).Select(i => i.ImagePath).FirstOrDefault();
                }

                // 2. Handle Image Archiving (Copying to Ordered Items Images)

                // 3. Category Tree String Builder
                string categoryTree = string.Join(" > ", fullProduct.categoriesorder.Select(c => c.CategoryName));

                decimal convertedUnitPrice = Math.Round(pkrUnitPrice / exchangeRate, 2);
                decimal convertedActualPrice = Math.Round(pkrActualPrice / exchangeRate, 2);

                checkoutData.Items.Add(new CartDetailViewModel
                {
                    ProductId = item.ProductId,
                    VariantId = item.VariantId,
                    Name = displayName,
                    ItemDetails = itemDetails, // Detailed string for snapshots
                    CategoryTree = categoryTree,
                    ImgPath = sourceImgPath,
                    ActualPrice = actualPrice,
                    Price = unitPrice,
                    Quantity = item.Quantity,
                    Total = unitPrice * item.Quantity,
                    OnSale = onSale,
                    DiscountRate = discountRate,
                    Description = description,
                    CurrencyType = currency
                });
            }

            checkoutData.GrandTotal = checkoutData.Items.Sum(x => x.Total);
            Session["CheckoutData"] = checkoutData;

            return Json(new { success = true, redirectUrl = Url.Action("Checkout", "Checkout", new { area = "EndUser" }) });
        }



        [HttpGet]
        public JsonResult GetCheckoutData()
        {
            string symbol = Request.Cookies["SelectedSymbol"]?.Value ?? "1";
            var checkoutData = Session["CheckoutData"] as CheckoutSessionModel;

            // 2. Check for Logged-in User via Cookie
            var userCookie = Request.Cookies["CustomerAuth"];
            object userData = null;

            if (userCookie != null)
            {
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
                user = userData,
                symbol = symbol
            }, JsonRequestBehavior.AllowGet);
        }

    }
}