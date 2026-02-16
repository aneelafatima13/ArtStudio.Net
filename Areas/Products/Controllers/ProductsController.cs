using BizOne.Common;
using BizOne.DAL;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace BizOne.Areas.Products.Controllers
{
    public class ProductsController : Controller
    {
        private readonly ProductsDAL dal = new ProductsDAL();
        // GET: Products/Profucts
        public ActionResult ManageProducts()
        {
            return View();
        }
        public ActionResult ProductDetails(long id)
        {
            ViewBag.ProductId = id;
            return View();
        }

        [HttpPost]
        public ActionResult AddProduct(Product product)
        {
            long empId = Convert.ToInt64(Session["EmpId"]);
            product.AddedBy = empId;
            product.ModifiedBy = empId;

            long productId = dal.ManageProducts(product, 1);

            SaveCategories(product.categoriesorder, productId, empId);

            string folderPath = GetProductFolderPath();
            EnsureFolderExists(folderPath);

            if (product.varients.Count > 1)
            {
                SaveMultipleVariants(product, productId, folderPath);
            }
            else
            {
                SaveSingleVariant(product, productId, folderPath);
            }

            return Json(new { success = true });
        }

        #region Helpers

        private void SaveCategories(IEnumerable<ProductCategory> categories, long productId, long empId)
        {
            foreach (var cat in categories)
            {
                cat.ProductId = productId;
                cat.AddedBy = empId;
                cat.ModifiedBy = empId;
                dal.ManageProductsCategories(cat, 1);
            }
        }

        private string GetProductFolderPath()
        {
            string username = Session["EmpUsername"] as string;
            string departmentName = Session["EmpDepartmentName"] as string;
            string basePath = Server.MapPath("~/Uploads/");

            return Path.Combine(basePath, departmentName, username, "ProductsImages");
        }

        private void EnsureFolderExists(string folderPath)
        {
            if (!Directory.Exists(folderPath))
                Directory.CreateDirectory(folderPath);
        }

        private void SaveImages(IEnumerable<ProductImage> images, string folderPath, long? productId = null, long? variantId = null,long? Addedby = null)
        {
            foreach (var img in images)
            {
                string fileName = Path.GetFileNameWithoutExtension(img.File.FileName);
                string extension = Path.GetExtension(img.File.FileName);
                string fullPath = Path.Combine(folderPath, img.File.FileName);

                int counter = 1;
                while (System.IO.File.Exists(fullPath))
                {
                    string newFileName = $"{fileName}({counter}){extension}";
                    fullPath = Path.Combine(folderPath, newFileName);
                    counter++;
                }

                img.File.SaveAs(fullPath);
                img.ImagePath = fullPath;
                img.ProductId = productId ?? 0;
                img.ProductVariantId = variantId ?? 0;
                img.AddedBy = Addedby;
                img.ModifiedBy = Addedby;

                dal.ManageProductsImages(img, 1);
            }
        }

        private void SaveSingleVariant(Product product, long productId, string folderPath)
        {
            var variant = product.varients.First();

            product.Id = productId;
            product.Colour = variant.Colour;
            product.Material = variant.Material;
            product.Size = variant.Size;
            product.OnSale = variant.OnSale;
            product.Price = variant.Price;
            product.DiscountPrice = variant.DiscountPrice;
            product.DiscountRate = variant.DiscountRate;
            product.IsAvailable = variant.IsAvailable;
            product.HasMoreVarients = false;
           
            if (variant.imageslist != null)
                SaveImages(variant.imageslist, folderPath, productId,null, (long)product.AddedBy);

            dal.ManageProducts(product, 2);
        }

        private void SaveMultipleVariants(Product product, long productId, string folderPath)
        {
            var firstVariant = product.varients.First();

            // Update product with first variant
            product.Id = productId;
            product.Colour = firstVariant.Colour;
            product.Material = firstVariant.Material;
            product.Size = firstVariant.Size;
            product.OnSale = firstVariant.OnSale;
            product.Price = firstVariant.Price;
            product.DiscountPrice = firstVariant.DiscountPrice;
            product.DiscountRate = firstVariant.DiscountRate;
            product.IsAvailable = firstVariant.IsAvailable;
            product.HasMoreVarients = true;

            dal.ManageProducts(product, 2);

            if (firstVariant.imageslist != null)
                SaveImages(firstVariant.imageslist, folderPath, productId,null, (long)product.AddedBy);

            foreach (var variant in product.varients.Skip(1))
            {
                variant.ProductId = productId;
                variant.AddedBy = product.AddedBy;
                variant.ModifiedBy = product.ModifiedBy;
                long variantId = dal.ManageProductsVarient(variant, 1);

                if (variant.imageslist != null)
                    SaveImages(variant.imageslist, folderPath, null, variantId, (long)product.AddedBy);
            }
        }

        #endregion

        [HttpPost]
        public JsonResult GetProducts()
        {
            int draw = Convert.ToInt32(Request.Form["draw"]);
            int start = Convert.ToInt32(Request.Form["start"]);
            int length = Convert.ToInt32(Request.Form["length"]);

            int pageNumber = (start / length) + 1;
            int pageSize = length;

            var (products, totalCount) = dal.GetProducts(pageNumber, pageSize);

            return Json(new
            {
                draw = draw,
                recordsTotal = totalCount,
                recordsFiltered = totalCount,
                data = products
            }, JsonRequestBehavior.AllowGet);

        }

        public ActionResult GetProductImage(string filePath)
        {
            return File(filePath, "image/png"); // adjust MIME type
        }
    }
}