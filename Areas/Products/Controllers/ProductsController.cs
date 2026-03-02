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
        public ActionResult ProductDetails(long id, string returnType)
        {
            ViewBag.ProductId = id;
            ViewBag.ReturnType = returnType;
            return View();
        }

        [HttpGet]
        public ActionResult GetProductDetails(long id)
        {
            try
            {
                // 1. Explicitly clear the session key before fetching new data
                Session.Remove("Productdetails");

                Product product = dal.GetProductDetailsById(id);

                if (product == null)
                {
                    return Json(null, JsonRequestBehavior.AllowGet);
                }

                // 2. Save the fresh product details
                Session["Productdetails"] = product;

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

        [HttpGet]
        public ActionResult GetProductImage(string path)
        {
            // Ensure the path is safe and return the file
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
            string basePath = Server.MapPath("~/Uploads/");

            return Path.Combine(basePath, "ProductsImages", username);
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
            product.StockQuantity = variant.StockQuantity;
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

        [HttpPost]
        public JsonResult UpdateProduct(Product product)
        {
            try
            {
                long currentUserId = Convert.ToInt64(Session["EmpId"]);
                product.AddedBy = currentUserId;
                product.ModifiedBy = currentUserId;
                
                // 1. Update Product Table
                dal.ManageProducts(product, 2);

                
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

     [HttpPost]
        public JsonResult UpdateCategory(long productId, string CategoryId, string ExistingMappingIds)
        {
            try
            {
                long currentUserId = Convert.ToInt64(Session["EmpId"]);
               
                // 2. Surgical Category Sync
                var newCatIds = (CategoryId ?? "").Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries).ToList();
                var oldMapIds = (ExistingMappingIds ?? "").Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries).ToList();

                int maxCount = Math.Max(newCatIds.Count, oldMapIds.Count);

                for (int i = 0; i < maxCount; i++)
                {
                    if (i < newCatIds.Count && i < oldMapIds.Count)
                    {
                        // UPDATE existing row at this index
                        dal.ManageProductsCategories(new ProductCategory
                        {
                            Id = long.Parse(oldMapIds[i]),
                            ProductId = productId,
                            CategoryId = long.Parse(newCatIds[i]),
                            ModifiedBy = currentUserId
                        }, 2); // Mode 2 = Update
                    }
                    else if (i < newCatIds.Count)
                    {
                        // INSERT new row (Hierarchy got deeper)
                        dal.ManageProductsCategories(new ProductCategory
                        {
                            ProductId = productId,
                            CategoryId = long.Parse(newCatIds[i]),
                            AddedBy = currentUserId
                        }, 1); // Mode 1 = Insert
                    }
                    else if (i < oldMapIds.Count)
                    {
                        // DELETE extra row (Hierarchy got shallower)
                        dal.ManageProductsCategories(new ProductCategory
                        {
                            Id = long.Parse(oldMapIds[i])
                        }, 3); // Mode 3 = Delete
                    }
                }

                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }
        [HttpPost]
        public JsonResult SetPrimaryImage(long imageId, long productId)
        {
            try
            {
                long empId = Convert.ToInt64(Session["EmpId"]);
                dal.ManageProductsImages(new ProductImage
                {
                    Id = imageId,
                    ProductId = productId,
                    IsPrimary = true,
                    ModifiedBy = empId
                }, 2);

                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        public JsonResult DeleteProductImage(long imageId, long pId, string imgPath)
        {
            try
            {
                
                if (imgPath != null)
                {
                    // 2. Delete physical file from folder
                    if (System.IO.File.Exists(imgPath))
                    {
                        System.IO.File.Delete(imgPath);
                    }

                    // 3. Delete record from Database (Mode 3)
                    dal.ManageProductsImages(new ProductImage { Id = imageId, ProductId = pId }, 3);
                }

                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        public JsonResult UploadProductImages(long productId)
        {
            try
            {
                // 1. Check if files were actually sent
                if (Request.Files.Count == 0)
                {
                    return Json(new { success = false, message = "No files selected." });
                }

                long empId = Convert.ToInt64(Session["EmpId"]);
                
                string folderPath = GetProductFolderPath(); // Your existing method
                EnsureFolderExists(folderPath); // Your existing method

                // 2. Map Request.Files to a list of ProductImage objects for your helper
                var imageList = new List<ProductImage>();
                for (int i = 0; i < Request.Files.Count; i++)
                {
                    var file = Request.Files[i];
                    if (file != null && file.ContentLength > 0)
                    {
                        imageList.Add(new ProductImage
                        {
                            File = file, // Assuming your ProductImage class has a HttpPostedFileBase property
                            IsPrimary = false // New uploads are secondary by default
                        });
                    }
                }

                // 3. Use your existing SaveImages helper to handle physical saving and DB insertion
                SaveImages(imageList, folderPath, productId, null, empId);

                
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        public JsonResult ToggleAvailability(long id, bool isAvailability)
        {
            try
            {
                long empId = Convert.ToInt64(Session["EmpId"]);
                
                Product product = new Product();
                product.IsAvailable = !isAvailability;
                product.ModifiedBy = empId;
                product.Id = id;
                dal.ManageProducts(product, 5);
                return Json(new { success = true, newState = product.IsAvailable });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        public JsonResult DeleteFullProduct(long id)
        {
            try
            {
                dal.ManageProducts(new Product { Id = id }, 3);
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        public JsonResult ToggleVariantAvailability(long variantId, bool currentStatus)
        {
            try
            {
                long empId = Convert.ToInt64(Session["EmpId"]);
                
                var variant = new ProductVariant
                {
                    Id = variantId,
                    IsAvailable = !currentStatus,
                    ModifiedBy = empId
                };

                dal.ManageProductsVarient(variant, 5);
                
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        public JsonResult DeleteVariant(long variantId)
        {
            try
            {
                // 1. Get product from session
                Product product = Session["Productdetails"] as Product;

                if (product == null || product.varients == null)
                {
                    // If session is lost, we can't get file paths safely from here
                    return Json(new { success = false, message = "Session expired. Please refresh." });
                }

                // 2. Find the specific variant in the session list
                var targetVariant = product.varients.FirstOrDefault(v => v.Id == variantId);

                if (targetVariant != null && targetVariant.imageslist != null)
                {
                    foreach (var img in targetVariant.imageslist)
                    {
                        if (!string.IsNullOrEmpty(img.ImagePath) && System.IO.File.Exists(img.ImagePath))
                        {
                            System.IO.File.Delete(img.ImagePath);
                        }
                    }
                }
                dal.ManageProductsVarient(new ProductVariant { Id = variantId }, 3);

                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        public JsonResult UploadVariantImages(long productId, long variantId)
        {
            try
            {
                var imageList = new List<ProductImage>();
                for (int i = 0; i < Request.Files.Count; i++)
                {
                    var file = Request.Files[i];
                    if (file != null && file.ContentLength > 0)
                    {
                        imageList.Add(new ProductImage { File = file, IsPrimary = false });
                    }
                }

                string folderPath = GetProductFolderPath(); // Same folder as product
                                                            // Note: passing variantId here to link them in DB (Mode 1)
                SaveImages(imageList, folderPath, productId, variantId, 1);

                return Json(new { success = true });
            }
            catch (Exception ex) { return Json(new { success = false, message = ex.Message }); }
        }

        [HttpPost]
        public JsonResult SetVariantPrimary(long imageId, long variantId)
        {
            try
            {
                long empId = Convert.ToInt64(Session["EmpId"]);

                dal.ManageProductsImages(new ProductImage
                {
                    Id = imageId,
                    ProductVariantId = variantId,
                    IsPrimary = true,
                    ModifiedBy = empId
                }, 2);

                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        public JsonResult DeleteVProductImage(long imageId,long variantId, string imgPath)
        {
            try
            {
                if (!string.IsNullOrEmpty(imgPath) && System.IO.File.Exists(imgPath))
                {
                    System.IO.File.Delete(imgPath);
                }

                dal.ManageProductsImages(new ProductImage { Id = imageId, ProductVariantId = variantId }, 7);

                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        public JsonResult UpdateVariantDetails(ProductVariant variant)
        {
            try
            {
                long empId = Convert.ToInt64(Session["EmpId"]);
                variant.ModifiedBy = empId;
                dal.ManageProductsVarient(variant, 2);
                
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        public JsonResult AddVariantWithImages(ProductVariant variant)
        {
            try
            {
                long empId = Convert.ToInt64(Session["EmpId"]);
                variant.AddedBy = empId;
                variant.ModifiedBy = empId;
                long newVariantId = dal.ManageProductsVarient(variant, 1);

                if (newVariantId > 0)
                {
                    // 2. Prepare the list of images for the helper method
                    var imageList = new List<ProductImage>();
                    for (int i = 0; i < Request.Files.Count; i++)
                    {
                        var file = Request.Files[i];
                        if (file != null && file.ContentLength > 0)
                        {
                            // For a brand NEW variant, we set the first image (index 0) as Primary
                            imageList.Add(new ProductImage
                            {
                                File = file,
                                IsPrimary = (i == 0),
                                AddedBy = empId,
                                ModifiedBy = empId
                            });
                        }
                    }

                    if (imageList.Count > 0)
                    {
                        string folderPath = GetProductFolderPath();
                        SaveImages(imageList, folderPath, variant.ProductId, newVariantId, 1);
                    }

                    return Json(new { success = true });
                }

                return Json(new { success = false, message = "Failed to create variant." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }
    }
}