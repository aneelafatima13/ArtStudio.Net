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

        public JsonResult Index(
     int page = 1,
     int pageSize = 12,
     long? categoryId = null,
     decimal? minPrice = null,
     decimal? maxPrice = null,
     bool? onSale = null,
     bool? notonSale = null,
     bool? newProducts = null,
     bool? inStock = null,
     string search = null,
     int? dRate = null,
     long? dPrice = null
    )
        {
            try
            {
                int totalRecords;

                var productList = dal.GetProducts(
                    page,
                    pageSize,
                    out totalRecords,
                    categoryId,
                    minPrice,
                    maxPrice,
                    onSale,
                    notonSale,
                    newProducts,
                    inStock,
                    search,
                    dRate,
                    dPrice
                );

                return Json(new
                {
                    success = true,
                    data = productList,
                    total = totalRecords,
                    page = page
                }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                
                return Json(new
                {
                    success = false,
                    message = "An error occurred while fetching products. Please try again later.",
                    errorDetails = ex.Message // Optional: remove in production for security
                }, JsonRequestBehavior.AllowGet);
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

        // 4. Fallback: Return a default "Image Not Found" placeholder
        return File(Server.MapPath("~/FurniAssets/images/product-1.png"), "image/png");
    }


    public ActionResult ProductDetails(long Id)
        {
            ViewBag.ProductId = Id;
            return View();
        }

        [HttpGet]
        public ActionResult GetProductDetails(long id)
        {
            try
            {
                // 1. Explicitly clear the session key before fetching new data
                Session.Remove("ProductdetailsByUser");

                Product product = pdal.GetProductDetailsById(id);

                if (product == null)
                {
                    return Json(null, JsonRequestBehavior.AllowGet);
                }

                // 2. Save the fresh product details
                Session["ProductdetailsByUser"] = product;

                return Json(new
                {
                    product = product,
                    imageslist = product.ProductImages,
                    varients = product.varients,
                    categories = product.categoriesorder
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

    }
}