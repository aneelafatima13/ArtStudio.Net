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

        public JsonResult Index(
            int page = 1,
            int pageSize = 12,
            long? categoryId = null,
            decimal? minPrice = null,
            decimal? maxPrice = null,
            string colour = null,
            string size = null,
            bool? onSale = null,
            string search = null)
        {
            int totalRecords;

            // Call the DAL method we created previously
            var productList = dal.GetProducts(
                page,
                pageSize,
                out totalRecords,
                categoryId,
                minPrice,
                maxPrice,
                colour,
                size,
                onSale,
                null, // InStock only (optional toggle)
                search
            );

            var model = new ShopViewModel
            {
                Products = productList,
                TotalCount = totalRecords,
                CurrentPage = page,
                PageSize = pageSize
            };

            // If it's an AJAX request (from your Filter Modal), return a Partial View
            return Json(new
            {
                data = model.Products,
                total = model.TotalCount,
                page = model.CurrentPage
            }, JsonRequestBehavior.AllowGet);
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

}
}