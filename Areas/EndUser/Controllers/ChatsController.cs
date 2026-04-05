using BizOne.BLL;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace BizOne.Areas.EndUser.Controllers
{
    public class ChatsController : Controller
    {
        ChatBLL bll = new ChatBLL();

        [HttpGet]
        public JsonResult GetChatHistory()
        {
            var customerIdStr = Request.Cookies["CustomerAuth"]?["UserId"];
            if (string.IsNullOrEmpty(customerIdStr)) return Json(new { success = false }, JsonRequestBehavior.AllowGet);

            long userId = long.Parse(customerIdStr);

            // customerId = userId, isAdminCalling = false, currentUserId = userId
            var data = bll.GetCustomerChat(userId, false, userId);

            return Json(new { data = data }, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public JsonResult SendMessage(string message, IEnumerable<HttpPostedFileBase> attachments)
        {
            try
            {
                var customerIdStr = Request.Cookies["CustomerAuth"]?["UserId"];
                if (string.IsNullOrEmpty(customerIdStr)) return Json(new { success = false, message = "Login required" });

                bool isSaved = bll.SaveMessage(long.Parse(customerIdStr), message, attachments);

                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error: " + ex.Message });
            }
        }
    }
}