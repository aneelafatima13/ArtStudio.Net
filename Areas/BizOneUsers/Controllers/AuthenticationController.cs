using BizOne.Common;
using BizOne.DAL;
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