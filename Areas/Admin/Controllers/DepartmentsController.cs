using BizOne.Controllers;
using BizOne.DAL;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace BizOne.Areas.Admin.Controllers
{
    public class DepartmentsController : BaseController
    {
        private readonly AdminDAL _departmentDAL = new AdminDAL();

        // GET: Admin/Roles
        public ActionResult ManageDeaprtments()
        {
            string userId = Session["EmpId"] as string;
            string username = Session["EmpUsername"] as string;
            _departmentDAL.AddActivityLog(long.Parse(userId), username + " has redirect to View Manage Departments.");

            return View();
        }

        // Save (Insert, Update, Delete)
        [HttpPost]
        public JsonResult SaveDeaprtment(int mode, long? id = null, string name = null)
        {
            string userId = Session["EmpId"] as string;
            string username = Session["EmpUsername"] as string;
            if (mode == 1 || mode == 2)
            {
                bool isrolefound = _departmentDAL.ManageDepartment(6, id, name, long.Parse(userId));
                if (isrolefound)
                {
                    return Json(new { success = false });
                }
            }
            _departmentDAL.ManageDepartment(mode, id, name, long.Parse(userId));
            if (mode == 1)
            {
                _departmentDAL.AddActivityLog(long.Parse(userId), username + " has added Department " + name);
            }
            else if (mode == 2)
            {
                _departmentDAL.AddActivityLog(long.Parse(userId), username + " has updated Department " + name);
            }
            else if (mode == 4)
            {
                _departmentDAL.AddActivityLog(long.Parse(userId), username + " has deleted Department " + name);
            }
            return Json(new { success = true });
        }

        // Get by Id
        [HttpGet]
        public JsonResult GetDeaprtmentById(long id)
        {
            var role = _departmentDAL.GetDepartmentById(id);
            return Json(role, JsonRequestBehavior.AllowGet);
        }

        // Server-side DataTable
        [HttpPost]
        public JsonResult GetDeaprtments()
        {
            int draw = Convert.ToInt32(Request.Form["draw"]);
            int start = Convert.ToInt32(Request.Form["start"]);
            int length = Convert.ToInt32(Request.Form["length"]);
            int pageNumber = (start / length) + 1;

            var (departments, totalCount) = _departmentDAL.GetDepartments(pageNumber, length);

            return Json(new
            {
                draw = draw,
                recordsTotal = totalCount,
                recordsFiltered = totalCount,
                data = departments
            }, JsonRequestBehavior.AllowGet);
        }

        [HttpGet]
        public ActionResult GetDepartmentsList()
        {
            AdminDAL dal = new AdminDAL();
            var rights = dal.GetDepartmentsList(); // this should return List<Rights>
            return Json(rights, JsonRequestBehavior.AllowGet);
        }

        [HttpGet]
        public JsonResult GetDepartmentsDropdown()
        {
            AdminDAL dal = new AdminDAL();
            var departments = dal.GetDepartmentsList();  // Your method
            return Json(departments, JsonRequestBehavior.AllowGet);
        }
    }
}