using BizOne.Common;
using BizOne.DAL;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace BizOne.Areas.Products.Controllers
{
    public class OrdersController : Controller
    {
        private readonly static ProductsDAL dal = new ProductsDAL();






        public ActionResult Orders(int returnType = 0)
        {
            ViewBag.ReturnType = returnType;
            var userCookie = Request.Cookies["CustomerAuth"];

            long? customerId = null;

            if (userCookie != null)
            {
                customerId = Convert.ToInt64(userCookie["UserId"]);

            }
            ViewBag.CustomerId = customerId;

            return View();
        }

        [HttpPost]
        public JsonResult FinalizeOrder(OrderModel model)
        {
            var cartData = Session["Cart"] as CartDetailViewModel;
            if (cartData == null || cartData.Items.Count == 0)
                return Json(new { success = false, message = "Cart is empty" });
            try
            {
                foreach(var data in cartData.Items)
                {
                    string archivedImgPath = ArchiveOrderImage(data.ImgPath);
                    data.CopyImgPath = archivedImgPath;

                }
                long orderId = dal.ExecuteFinalizeOrder(model, cartData);

                if (orderId > 0)
                {
                    // IMPORTANT: Clear the cart now that the order is placed
                    Session["Cart"] = null;
                    Session["CheckoutData"] = null;

                    return Json(new { success = true, orderId = orderId });
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error: " + ex.Message });
            }

            return Json(new { success = false, message = "Could not process order." });
        }

        private string ArchiveOrderImage(string sourceRelativePath)
        {
            if (string.IsNullOrEmpty(sourceRelativePath)) return "/Uploads/no-image.png";

            try
            {
                // 1. Prepare naming
                string originalFileName = Path.GetFileName(sourceRelativePath);
                string newFileName = originalFileName; // e.g., 638452_product.jpg

                // 2. Physical Paths (For System.IO)
                string targetFolder = Server.MapPath("~/Uploads/OrderedItemsImages/");
                string targetPhysicalPath = Path.Combine(targetFolder, newFileName);

                if (!Directory.Exists(targetFolder)) Directory.CreateDirectory(targetFolder);

                
                string sourcePhysicalPath = sourceRelativePath.Contains(":")
                    ? sourceRelativePath
                    : Server.MapPath(sourceRelativePath);

                if (System.IO.File.Exists(sourcePhysicalPath))
                {
                    if (!System.IO.File.Exists(targetPhysicalPath))
                    {
                        System.IO.File.Copy(sourcePhysicalPath, targetPhysicalPath, true);
                    }
                    return targetPhysicalPath;

                }
                else
                {
                    return sourceRelativePath;
                }
            }
            catch
            {
                return sourceRelativePath;
            }
        }

        // GET: Products/Orders
        [HttpGet]
        public JsonResult GetCustomerOrders(long customerId, int pageNumber = 1, int pageSize = 10)
        {
            var orders = dal.GetOrdershistorybyCustomerId(customerId,pageNumber,pageSize);

            return Json(new { data = orders }, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public JsonResult FinalizeCustomeOrder(CustomeOrderModel model)
        {
            try
            {
                List<string> savedPaths = new List<string>();

                if (model.ReferenceFiles != null && model.ReferenceFiles.Count > 0)
                {
                    var userCookie = Request.Cookies["CustomerAuth"];
                    // Clean the name to prevent invalid folder characters
                    string cusName = userCookie != null ? userCookie["FullName"].Replace(" ", "_") : "Guest";

                    // 1. Define folder paths
                    string folderVirtualPath = $"/Uploads/CustomeOrdersFiles/{cusName}/";
                    string folderPhysicalPath = Server.MapPath("~" + folderVirtualPath);

                    if (!Directory.Exists(folderPhysicalPath))
                        Directory.CreateDirectory(folderPhysicalPath);

                    foreach (var file in model.ReferenceFiles)
                    {
                        if (file != null && file.File.ContentLength > 0)
                        {
                            // 2. Make filename unique to prevent overwriting
                            string fileName = Guid.NewGuid().ToString().Substring(0, 8) + "_" + Path.GetFileName(file.File.FileName);
                            string physicalSavePath = Path.Combine(folderPhysicalPath, fileName);

                            // 3. Save the file
                            file.File.SaveAs(physicalSavePath);

                            // 4. ADD THE VIRTUAL PATH (URL) TO THE LIST
                            // This is what goes to the DB so your website can display it
                            savedPaths.Add(folderVirtualPath + fileName);
                        }
                    }
                }

                model.ReferenceFilesPaths = JsonConvert.SerializeObject(savedPaths);
                long orderId = dal.ExecuteFinalizeCustomeOrder(model);

                if (orderId > 0)
                {
                   return Json(new { success = true, orderId = orderId });
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Server Error: " + ex.Message });
            }
            return Json(new { success = false, message = "Could not process order." });
        }
    
    }
}