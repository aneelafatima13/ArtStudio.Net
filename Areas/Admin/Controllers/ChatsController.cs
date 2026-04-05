using BizOne.BLL;
using BizOne.Controllers;
using BizOne.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace BizOne.Areas.Admin.Controllers
{
    public class ChatsController : BaseController
    {
        ChatBLL bll = new ChatBLL();

        public ActionResult Chats()
        {
            return View();
        }
        [HttpGet]
        public JsonResult GetCustomersList()
        {
            // Get logged in Admin ID from your Auth system/Session
            long currentAdminId = LoginUser.EmpId;

            // Update your BLL/DAL to accept this ID for the sidebar query
            var list = bll.GetAdminChatSidebar(currentAdminId);

            return Json(list, JsonRequestBehavior.AllowGet);
        }

        [HttpGet]
        public JsonResult GetChatHistory(long id) // 'id' is the CustomerId passed from JS
        {
            // Get the logged-in Admin's ID from Session or your Authentication context
            long adminId = LoginUser.EmpId;

            // isAdminCalling = true
            var data = bll.GetCustomerChat(id, true, adminId);

            return Json(new { data = data }, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public JsonResult SendMessage(string message, IEnumerable<HttpPostedFileBase> attachments, string customerIdStr)
        {
            try
            {
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