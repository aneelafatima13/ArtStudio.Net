using System.Web.Mvc;

namespace BizOne.Areas.BizOneUsers
{
    public class BizOneUsersAreaRegistration : AreaRegistration
    {
        public override string AreaName
        {
            get { return "BizOneUsers"; }
        }

        public override void RegisterArea(AreaRegistrationContext context)
        {
            context.MapRoute(
                "BizOneUsers_default",
                "BizOneUsers/{controller}/{action}/{id}",
                new { action = "Index", id = UrlParameter.Optional }
            );
        }
    }
}
