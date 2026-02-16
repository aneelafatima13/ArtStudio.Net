using BizOne.Controllers;
using BizOne.DAL;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace BizOne.Areas.Admin.Controllers
{
    public class RolesController : BaseController
    {
        private readonly AdminDAL _roleDAL = new AdminDAL();

        // GET: Admin/Roles
        public ActionResult ManageRoles()
        {
            string userId = Session["EmpId"] as string;
            string username = Session["EmpUsername"] as string;
            _roleDAL.AddActivityLog(long.Parse(userId), username + " has redirect to View Manage Roles.");

            return View();
        }
       
        // Save (Insert, Update, Delete)
        [HttpPost]
        public JsonResult SaveRole(int mode, long? id = null, string name = null)
        {
            string userId = Session["EmpId"] as string;
            string username = Session["EmpUsername"] as string;
            if (mode == 1 || mode == 2)
            {
                bool isrolefound = _roleDAL.ManageRole(6, id, name, long.Parse(userId));
                if (isrolefound)
                {
                    return Json(new { success = false });
                }
            }
            _roleDAL.ManageRole(mode, id, name, long.Parse(userId));
            if (mode == 1)
            {
                _roleDAL.AddActivityLog(long.Parse(userId), username + " has added Role " + name);
            }
            else if (mode == 2)
            {
                _roleDAL.AddActivityLog(long.Parse(userId), username + " has updated Role " + name);
            }
            else if (mode == 4)
            {
                _roleDAL.AddActivityLog(long.Parse(userId), username + " has deleted Role " + name);
            }
            return Json(new { success = true });
        }

        // Get by Id
        [HttpGet]
        public JsonResult GetRoleById(long id)
        {
            var role = _roleDAL.GetRoleById(id);
            return Json(role, JsonRequestBehavior.AllowGet);
        }

        // Server-side DataTable
        [HttpPost]
        public JsonResult GetRoles()
        {
            int draw = Convert.ToInt32(Request.Form["draw"]);
            int start = Convert.ToInt32(Request.Form["start"]);
            int length = Convert.ToInt32(Request.Form["length"]);
            int pageNumber = (start / length) + 1;

            var (roles, totalCount) = _roleDAL.GetRoles(pageNumber, length);

            return Json(new
            {
                draw = draw,
                recordsTotal = totalCount,
                recordsFiltered = totalCount,
                data = roles
            }, JsonRequestBehavior.AllowGet);
        }
    }
}
