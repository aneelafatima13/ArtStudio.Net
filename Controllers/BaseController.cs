using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace BizOne.Controllers
{

    public class BaseController : Controller
    {
        protected override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            // Check session
            if (Session["EmpId"] == null)
            {
                TempData["SessionExpired"] = "Your session has expired. Please log in again.";

                filterContext.Result = new RedirectToRouteResult(
                    new System.Web.Routing.RouteValueDictionary {
                        { "area", "BizOneUsers" },
                        { "controller", "Authentication" },
                        { "action", "Login" }
                    }
                );

            }

            base.OnActionExecuting(filterContext);
        }
    }

}