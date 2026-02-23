using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace BizOne.Areas.EndUser.Controllers
{
    public class LocalHomeController : Controller
    {
        // GET: LocalHome
        public ActionResult Home()
        {
            return View();
        }

        public ActionResult Shop()
        {
            return View();
        }

        public ActionResult AboutUs()
        {
            return View();
        }

        public ActionResult Services()
        {
            return View();
        }

        public ActionResult Blog()
        {
            return View();
        }

        public ActionResult ContactUs()
        {
            return View();
        }

        public ActionResult Checkout()
        {
            return View();
        }
        

        public ActionResult Cart()
        {
            return View();
        }
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

        public ActionResult CustomDesigns()
        {
            
            return View();
        }
    }
}