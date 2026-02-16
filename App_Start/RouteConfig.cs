using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;

namespace BizOne
{
    public class RouteConfig
    {
        public static void RegisterRoutes(RouteCollection routes)
        {
            routes.IgnoreRoute("{resource}.axd/{*pathInfo}");

            // Default route (when no area specified)
            routes.MapRoute(
                name: "Default",
                url: "{controller}/{action}/{id}",
                defaults: new
                {
                    area = "BizOneUsers",
                    controller = "Authentication",
                    action = "MainView",
                    id = UrlParameter.Optional
                }
            ).DataTokens.Add("area", "BizOneUsers");
        }
    }

}
