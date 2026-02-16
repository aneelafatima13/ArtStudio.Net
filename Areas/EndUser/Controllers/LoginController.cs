using BizOne.DAL;
using Google.Apis.Auth;
using System;
using System.Data;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;

namespace BizOne.Areas.EndUser.Controllers
{
    public class LoginController : Controller
    {
        private static readonly CustomersDAL dal = new CustomersDAL();
        // GET: EndUser/Login
        public ActionResult Login()
        {
            return View();
        }

        [HttpPost]
        public JsonResult SubmitLogin(string Username, string Password)
        {
            DataTable dt = dal.LoginUser(Username, Password);

            // If Id is greater than 0, login is valid
            if (dt.Rows.Count > 0 && Convert.ToInt64(dt.Rows[0]["Id"]) > 0)
            {
                var row = dt.Rows[0];

                HttpCookie userCookie = new HttpCookie("CustomerAuth");
                userCookie["UserId"] = row["Id"].ToString();
                userCookie["FullName"] = row["FullName"].ToString();
                userCookie["Email"] = row["Email"].ToString();
                userCookie.Expires = DateTime.Now.AddDays(7);
                Response.Cookies.Add(userCookie);

                return Json(new { success = true, message = "Welcome back!" });
            }

            return Json(new { success = false, message = "Invalid Username, Password, or account inactive." });
        }

        public ActionResult SignUp()
        {
            return View();
        }

        [HttpPost]
        public JsonResult SubmitSignUp(string FullName, string UserName, string Email, string PhoneNumber, string Password)
        {
            try
            {
                long result = dal.RegisterUser(FullName, UserName, Email, PhoneNumber, Password);
                return Json(result, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                // Log error here
                return Json(0);
            }
        }

        [HttpPost]
        public async Task<JsonResult> GoogleSignUp(string idToken)
        {
            try
            {
                var payload = await GoogleJsonWebSignature.ValidateAsync(idToken);
                DataTable dt = dal.GetCustomerByEmail(payload.Email);

                bool isNewUser = false;
                string genUser = "";
                string genPass = "";

                if (dt.Rows.Count == 0)
                {
                    isNewUser = true;
                    genUser = payload.Email.Split('@')[0] + "_" + new Random().Next(100, 999);
                    genPass = "ArtNest_" + Guid.NewGuid().ToString().Substring(0, 8);

                    dal.RegisterUser(payload.Name, genUser, payload.Email, "", genPass);
                    dt = dal.GetCustomerByEmail(payload.Email);
                }

                // Create the session cookie
                HttpCookie userCookie = new HttpCookie("CustomerAuth");
                userCookie["UserId"] = dt.Rows[0]["Id"].ToString();
                userCookie["FullName"] = dt.Rows[0]["FullName"].ToString();
                userCookie["Email"] = dt.Rows[0]["Email"].ToString();
                userCookie.Expires = DateTime.Now.AddDays(7);
                Response.Cookies.Add(userCookie);

                return Json(new
                {
                    success = true,
                    isNewUser = isNewUser,
                    generatedUser = genUser,
                    generatedPass = genPass,
                    message = "Successfully authenticated as " + payload.Name
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        public ActionResult Logout()
        {
            if (Request.Cookies["CustomerAuth"] != null)
            {
                HttpCookie myCookie = new HttpCookie("CustomerAuth");
                myCookie.Expires = DateTime.Now.AddDays(-1d); // Set expiration to yesterday
                Response.Cookies.Add(myCookie);
            }
            return RedirectToAction("Home", "LocalHome", new { area = "EndUser" });
        }
    }
}