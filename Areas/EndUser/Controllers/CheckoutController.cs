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

        [HttpGet]
        public JsonResult GetCheckoutData()
        {
            string symbol = Request.Cookies["SelectedSymbol"]?.Value ?? "1";
            var cartData = Session["Cart"] as CartDetailViewModel;

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
                cart = cartData,
                user = userData,
                symbol = symbol
            }, JsonRequestBehavior.AllowGet);
        }

    }
}