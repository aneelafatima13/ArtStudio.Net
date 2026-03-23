using BizOne.Controllers;
using BizOne.DAL;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace BizOne.Areas.Products.Controllers
{
    public class AdminOrdersController : BaseController
    {
        ProductsDAL dal = new ProductsDAL();
        // GET: Products/AdminOrders
        public ActionResult ManageOrders(int returnType)
        {
            ViewBag.ReturnType = returnType;
            return View();
        }

        [HttpGet]
        public JsonResult GetOrders(string search = "", int page = 1, int mode = 1)
        {
            try
            {
                int pageSize = 10;

                // Call the separate DB method
                var result = dal.FetchOrderSummariesFromDb(search, page, pageSize, mode);

                return Json(new
                {
                    success = true,
                    data = result.Orders,
                    total = result.TotalCount,
                    pages = Math.Ceiling((double)result.TotalCount / pageSize)
                }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                // Log your exception here
                return Json(new { success = false, message = "An error occurred while fetching orders." }, JsonRequestBehavior.AllowGet);
            }
        }
        public ActionResult OrderDetails(string Email, int returnType)
        {
            ViewBag.Email = Email;
            ViewBag.ReturnType = returnType;
            return View();
        }

        [HttpGet]
        public JsonResult GetCustomerOrderData(string email)
        {
            try
            {
                var data = dal.GetFullOrderHistory(email);
                return Json(new { success = true, data = data }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }



    }
}