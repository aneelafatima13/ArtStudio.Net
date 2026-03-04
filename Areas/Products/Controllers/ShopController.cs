using BizOne.Common;
using BizOne.DAL;
using Microsoft.AspNetCore.Hosting.Server;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace BizOne.Areas.Products.Controllers
{
    public class ShopController : Controller
    {
        private readonly ShopDAL dal = new ShopDAL();
        private readonly ProductsDAL pdal = new ProductsDAL();

        [HttpPost]
        public JsonResult GetCategories()
        {
            try
            {
                var categories = pdal.CategoriesList(7);

                if (categories == null)
                {
                    return Json(new { success = false, message = "No categories found." });
                }

                // Return an anonymous object for better control
                return Json(new
                {
                    success = true,
                    data = categories
                }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                // Log your exception here
                return Json(new { success = false, message = "An error occurred." });
            }
        }

        // Load child categories
        [HttpPost]
        public JsonResult GetSubCategories(long parentId)
        {
            var categories = pdal.CategoriesList(8, id: parentId); // Mode 8 = get children
            return Json(categories, JsonRequestBehavior.AllowGet);
        }

        public JsonResult Index(int page = 1, int pageSize = 12, long? categoryId = null, decimal? minPrice = null, decimal? maxPrice = null, bool? onSale = null, bool? notonSale = null, bool? newProducts = null, bool? inStock = null, string search = null, int? dRate = null, long? dPrice = null)
        {
            try
            {
                string currency = Request.Cookies["SelectedCurrency"]?.Value ?? "PKR";
                string symbol = Request.Cookies["SelectedSymbol"]?.Value ?? "Rs.";
                string rateStr = Request.Cookies["SelectedRate"]?.Value ?? "1";

                decimal exchangeRate = decimal.Parse(rateStr);

                int totalRecords;
                var productList = dal.GetProducts(page, pageSize, out totalRecords, categoryId, minPrice, maxPrice, onSale, notonSale, newProducts, inStock, search, dRate, dPrice);

                foreach (var p in productList)
                {
                    if (currency == "USD")
                    {
                        // Convert Base Price
                        p.ConvertedPrice = p.Price.HasValue ? Math.Round(p.Price.Value / exchangeRate, 2) : 0;

                        // Convert Discount Price (if exists)
                        if (p.DiscountPrice.HasValue && p.DiscountPrice > 0)
                        {
                            p.ConvertedDiscountPrice = Math.Round(p.DiscountPrice.Value / exchangeRate, 2);
                        }
                    }
                    else
                    {
                        p.ConvertedPrice = p.Price;
                        p.ConvertedDiscountPrice = p.DiscountPrice;
                    }
                    if (p.HasMoreVarients)
                    {
                        foreach (var v in p.varients)
                        {
                            if (currency == "USD")
                            {
                                // Convert Base Price
                                v.ConvertedPrice = v.Price.HasValue ? Math.Round(v.Price.Value / exchangeRate, 2) : 0;

                                // Convert Discount Price (if exists)
                                if (v.DiscountPrice.HasValue && v.DiscountPrice > 0)
                                {
                                    v.ConvertedDiscountPrice = Math.Round(v.DiscountPrice.Value / exchangeRate, 2);
                                }
                            }
                            else
                            {
                                v.ConvertedPrice = v.Price;
                                v.ConvertedDiscountPrice = v.DiscountPrice;
                            }
                        }
                    }
                }

                return Json(new
                {
                    success = true,
                    data = productList,
                    total = totalRecords,
                    page = page,
                    currencySymbol = symbol,
                }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error fetching products.", errorDetails = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        [HttpGet]
    public ActionResult GetImg(string path)
    {
        try
        {
            // 1. Check if path is empty
            if (string.IsNullOrWhiteSpace(path))
            {
                return File(Server.MapPath("~/FurniAssets/images/product-1.png"), "image/png");
            }

            string physicalPath = path;

            // 3. Security & Existence Check
            if (System.IO.File.Exists(physicalPath))
            {
                // Detects MIME type (image/jpeg, image/png, etc.) automatically
                string contentType = MimeMapping.GetMimeMapping(physicalPath);
                return File(physicalPath, contentType);
            }
        }
        catch (Exception ex)
        {
            // Log your exception here: Elmah, NLog, etc.
        }

        return File(Server.MapPath("~/FurniAssets/images/product-1.png"), "image/png");
    }


    public ActionResult ProductDetails(long Id, long? VarientId)
        {
            ViewBag.ProductId = Id;
            ViewBag.VarientId = VarientId;
            return View();
        }

        [HttpGet]
        public ActionResult GetProductDetails(long id)
        {
            try
            {
                // 1. Read Cookies (Set by the Navbar)
                string currency = Request.Cookies["SelectedCurrency"]?.Value ?? "PKR";
                string symbol = Request.Cookies["SelectedSymbol"]?.Value ?? "Rs.";
                string rateStr = Request.Cookies["SelectedRate"]?.Value ?? "1";

                // Parse rate safely; default to 1 if PKR or cookie missing
                decimal exchangeRate = decimal.TryParse(rateStr, out decimal r) ? r : 1.0m;

                // 2. Fetch Data
                Session.Remove("Productdetails");
                Product product = pdal.GetProductDetailsById(id);

                if (product == null) return Json(null, JsonRequestBehavior.AllowGet);

                // 3. Dynamic Conversion Logic
                // Convert Main Product Prices
                product.ConvertedPrice = product.Price.HasValue
                    ? Math.Round(product.Price.Value / exchangeRate, 2) : 0;

                product.ConvertedDiscountPrice = (decimal?)((product.DiscountPrice.HasValue && product.DiscountPrice > 0)
                    ? Math.Round(product.DiscountPrice.Value / exchangeRate, 2)
                    : product.ConvertedPrice);

                // Convert Variant Prices
                if (product.varients != null)
                {
                    foreach (var v in product.varients)
                    {
                        v.ConvertedPrice = v.Price.HasValue
                            ? Math.Round(v.Price.Value / exchangeRate, 2) : 0;

                        v.ConvertedDiscountPrice = (v.DiscountPrice.HasValue && v.DiscountPrice > 0)
                            ? Math.Round(v.DiscountPrice.Value / exchangeRate, 2)
                            : v.ConvertedPrice;
                    }
                }

                Session["Productdetails"] = product;

                return Json(new
                {
                    product = product,
                    imageslist = product.ProductImages,
                    varients = product.varients,
                    symbol = symbol // Send symbol to Frontend
                }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { error = "Internal Server Error" }, JsonRequestBehavior.AllowGet);
            }
        }
        
        
        [HttpGet]
        public ActionResult ReadProductDetailsfromSession()
        {
            try
            {
                Product product = Session["Productdetails"] as Product;

                if (product == null)
                {
                    return Json(null, JsonRequestBehavior.AllowGet);
                }

                return Json(new
                {
                    product = product,
                    imageslist = product.ProductImages,
                    varients = product.varients,
                    categories = product.categoriesorder // Add this to show category breadcrumbs
                }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                // Log exception (ex) here
                return Json(new { error = "Internal Server Error" }, JsonRequestBehavior.AllowGet);
            }
        }

        public ActionResult GetCurrencyDropdown()
        {
            int total;
            // Mode 6: Fetch all records without paging
            var list = pdal.GetExchangeRates(6, null, 0, 0, out total);

            // Check if a cookie already exists to set the label
            var selected = Request.Cookies["SelectedCurrency"]?.Value ?? "PKR";
            
            return Json(new { success = true, data = list }, JsonRequestBehavior.AllowGet);   
        }
    }
}