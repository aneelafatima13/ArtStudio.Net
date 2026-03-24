using BizOne.Common;
using BizOne.Controllers;
using BizOne.DAL;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace BizOne.Areas.Admin.Controllers
{
    public class RightsController : BaseController
    {
        public static AdminDAL dal = new AdminDAL();
        // GET: Admin/Rights
        public ActionResult ManageRights()
        {
            
            string userId = Session["EmpId"] as string;
            string username = Session["EmpUsername"] as string;
            dal.AddActivityLog(long.Parse(userId), username + " has redirect to View Manage Rights.");
            return View();
        }

        [HttpPost]
        public ActionResult SaveRights(List<Rights> rights, int mode)
        {
            if (rights != null && rights.Count > 0)
            {
                foreach (var right in rights)
                {
                    dal.AddupdateRights(right, mode);
                }
                return Json(new { success = true });
            }
            return Json(new { success = false });
        }

        [HttpGet]
        public ActionResult GetRights()
        {
            var rights = dal.GetAllRights(); // this should return List<Rights>
            return Json(rights, JsonRequestBehavior.AllowGet);
        }

        public ActionResult AssignRights()
        {
            string userId = Session["EmpId"] as string;
            string username = Session["EmpUsername"] as string;
            dal.AddActivityLog(long.Parse(userId), username + " has redirect to Assign Rights.");
            return View();
        }

        [HttpGet]
        public JsonResult GetRolesDropdown()
        {
            var roles = dal.GetRolesList();  // Your method
            return Json(roles, JsonRequestBehavior.AllowGet);
        }

        [HttpGet]
        public JsonResult GetAssignedRights(long roleId)
        {
           var rights = dal.GetRightsByRole(roleId);
            return Json(rights, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public JsonResult SaveAssignedRights(long roleId, List<long> selectedRightIds)
        {
            string userId = Session["EmpId"] as string;
            string username = Session["EmpUsername"] as string; // replace with actual logged-in user id
            
            // First, get all existing rights for role
            var existingRights = dal.GetRightsByRole(roleId);
            var existingRightIds = new HashSet<long>();
            foreach (var r in existingRights) existingRightIds.Add(r.RightId);

            if (selectedRightIds != null && selectedRightIds.Count > 0)
            {
                // Update or insert each selected right
                foreach (var rightId in selectedRightIds)
                {
                    dal.SaveOrUpdateRight(roleId, rightId, true, long.Parse(userId));
                    existingRightIds.Remove(rightId);
                }
            }
            // For any rights not selected, set IsActive = false
            foreach (var rightId in existingRightIds)
            {
                dal.SaveOrUpdateRight(roleId, rightId, false, long.Parse(userId));
            }

            
            dal.AddActivityLog(long.Parse(userId), username + " has Assign Rights.");

            return Json(new { success = true });
        }

    }
}