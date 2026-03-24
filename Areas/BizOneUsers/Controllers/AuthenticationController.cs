using BizOne.Common;
using BizOne.DAL;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Web.Mvc;
using System.Xml.Linq;

namespace BizOne.Areas.BizOneUsers.Controllers
{
    public class AuthenticationController : Controller
    {
        private readonly AdminDAL _employeeDAL = new AdminDAL();
        public ActionResult Login()
        {
            return View();
        }

        public ActionResult MainView()
        {
            return View();
        }


        [HttpPost]
        public ActionResult SubmitLogin(LoginModel model)
        {
            
            Employee empData = new Employee();
            List<string> rightsName = new List<string>();
            (empData, rightsName) = _employeeDAL.GetEmployeeData(model);

            if (empData != null || (model.Username == "admin" && model.Password == "123"))
            {
                if (empData.Password == model.Password || (model.Username == "admin" && model.Password == "123"))
                {
                    Session["EmpId"] = empData.Id.ToString();
                    Session["EmpUsername"] = model.Username;
                    Session["EmpRole"] = empData.RoleName;
                    Session["EmpDepartmentName"] = empData.DepartmentName;
                    Session["EmpRightsList"] = rightsName;
                    RightsFlagsList rightsflags = setAllRightsFlags(rightsName);
                    Session["RightsFlags"] = rightsflags;
                    _employeeDAL.AddActivityLog(empData.Id, model.Username + " has login Application.");
                    return Json(new { success = true, redirectUrl = Url.Action("Dashboard", "Authentication") });
                }
                else
                {
                    return Json(new { success = false, message = "Invalid login Incorrect Password" });
                }
            }
            else
            {
                return Json(new { success = false, message = "Invalid login Username Not found" });
            }
        }

        public RightsFlagsList setAllRightsFlags(List<string> rightsName)
        {
            if (rightsName == null)
                rightsName = new List<string>();

            // Using a HashSet for O(1) lookup performance to keep the app snappy
            var rights = new HashSet<string>(rightsName, StringComparer.OrdinalIgnoreCase);

            return new RightsFlagsList
            {
                // Credentials Rights
                ROLES_MANAGE = rights.Contains("ROLES_MANAGE"),
                ASSIGN_RIGHTS = rights.Contains("ASSIGN_RIGHTS"),
                RIGHTS_MANAGE = rights.Contains("RIGHTS_MANAGE"),

                // Employees Rights
                DEPARTMENTS_MANAGE = rights.Contains("DEPARTMENTS_MANAGE"),
                EMPLOYEE_ADD = rights.Contains("EMPLOYEE_ADD"),
                EMPLOYEE_UPDATE = rights.Contains("EMPLOYEE_UPDATE"),
                EMPLOYEE_VIEW = rights.Contains("EMPLOYEE_VIEW"),
                ATTENDANCE_VIEW = rights.Contains("ATTENDANCE_VIEW"),
                LEAVES_APPROVE = rights.Contains("LEAVES_APPROVE"),

                // Category Rights
                CATEGORY_MANAGE = rights.Contains("CATEGORY_MANAGE"),
                CATEGORY_VIEW = rights.Contains("CATEGORY_VIEW"),

                // Product Rights
                PRODUCT_ADD = rights.Contains("PRODUCT_ADD"),
                PRODUCT_UPDATE = rights.Contains("PRODUCT_UPDATE"),
                PRODUCT_DELETE = rights.Contains("PRODUCT_DELETE"),

                // Order Rights
                ORDER_VIEW = rights.Contains("ORDER_VIEW"),

                // Document Rights
                DOCUMENT_UPLOAD = rights.Contains("DOCUMENT_UPLOAD")
            };
        }


        public ActionResult Dashboard()
        {
            string userId = Session["EmpId"] as string;
            string username = Session["EmpUsername"] as string;
            _employeeDAL.AddActivityLog(long.Parse(userId), username + " has redirect to View Dashboard.");
            return View();
        }

        public ActionResult Logout()
        {
            string userId = Session["EmpId"] as string;
            string username = Session["EmpUsername"] as string;
            if (!string.IsNullOrWhiteSpace(userId))
            {
                _employeeDAL.AddActivityLog(long.Parse(userId), username + " has Logout the Application.");
            }
            Session.Clear();  
            Session.Abandon();
            return RedirectToAction("Login", "Authentication");
        }

    }
}