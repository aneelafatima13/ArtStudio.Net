using BizOne.Common;
using BizOne.Controllers;
using BizOne.DAL;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace BizOne.Areas.Products.Controllers
{
    public class CoupensController : BaseController
    {
        private static readonly CustomersDAL dal = new CustomersDAL();
        private static readonly ProductsDAL pdal = new ProductsDAL();
        // GET: Products/Coupens
        public ActionResult ManageCoupens()
        {
            return View();
        }

        [HttpGet]
        public JsonResult VerifyCustomer(string email)
        {
            // Fetch the data into a DataTable
            DataTable dt = dal.GetCustomerByEmail(email);

            if (dt != null && dt.Rows.Count > 0)
            {
                // Extract data from the first row
                var row = dt.Rows[0];
                return Json(new
                {
                    success = true,
                    exists = true,
                    userId = row["Id"], // Assuming column name is 'Id'
                    userName = row["FullName"] // Assuming column name is 'FullName'
                }, JsonRequestBehavior.AllowGet);
            }

            return Json(new { success = false, exists = false }, JsonRequestBehavior.AllowGet);
        }
        
        [HttpPost]
        public JsonResult SaveCoupon(CouponModel model)
        {
            try
            {
                long empId = Convert.ToInt64(Session["EmpId"]);

                if (model.Id == 0)
                    model.CreatedById = empId;
                else
                    model.ModifiedById = empId;

                bool result = pdal.SaveCoupon(model);
                return Json(new { success = result, message = result ? "Saved successfully!" : "Error saving coupon." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        public JsonResult GetCouponsList()
        {
            int draw = Convert.ToInt32(Request.Form["draw"]);
            int start = Convert.ToInt32(Request.Form["start"]);
            int length = Convert.ToInt32(Request.Form["length"]);

            // Calculate current page for DAL
            int pageNumber = (start / length) + 1;
            int totalRecords = 0;

            var data = pdal.GetPaginatedCoupons(pageNumber, length, out totalRecords);

            return Json(new
            {
                draw = draw,
                recordsTotal = totalRecords,
                recordsFiltered = totalRecords,
                data = data
            });
        }
    
        // 3. Get Single Coupon for Edit/View (Mode 4)
        [HttpGet]
        public JsonResult GetCouponById(long id)
        {
            // Use a mode in your DAL to fetch a single row by ID
            CouponModel coupon = pdal.GetCouponByIdorCode(id, 4, null);
            return Json(coupon, JsonRequestBehavior.AllowGet);
        }
        
        // 4. Toggle Active Status (New Mode 6 in SP)
        [HttpPost]
        public JsonResult ToggleCouponStatus(long id)
        {
            try
            {
                bool isUpdated = pdal.UpdateStatus(id);
                return Json(new { success = isUpdated, message = isUpdated ? "Status changed" : "Failed to update" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // 5. Delete Coupon (Mode 3)
        [HttpPost]
        public JsonResult DeleteCoupon(long id)
        {
            try
            {
                bool result = pdal.DeleteCoupon(id);
                return Json(new { success = result, message = result ? "Deleted successfully" : "Delete failed" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpGet]
        public JsonResult VarifyCouponCode(string code)
        {
            // 1. Fetch Coupon Data
            CouponModel coupon = pdal.GetCouponByIdorCode(null, 7, code);

            if (coupon == null || coupon.Id == 0)
            {
                return Json(new { success = false, message = "Invalid coupon code." }, JsonRequestBehavior.AllowGet);
            }

            // 2. Check if Active
            if (!coupon.IsActive)
            {
                return Json(new { success = false, message = "This coupon is no longer active." }, JsonRequestBehavior.AllowGet);
            }

            // 3. Check Expiry Date
            if (!string.IsNullOrEmpty(coupon.ExpiryDate))
            {
                DateTime expiry = DateTime.Parse(coupon.ExpiryDate);
                if (DateTime.Now.Date > expiry.Date)
                {
                    return Json(new { success = false, message = "This coupon has expired." }, JsonRequestBehavior.AllowGet);
                }
            }

            // 4. Check Usage Limit
            if (coupon.UsageLimit <= 0) // Assuming usage limit decrements or is compared against a count
            {
                return Json(new { success = false, message = "Usage limit reached for this coupon." }, JsonRequestBehavior.AllowGet);
            }

            // 5. Check Customer Ownership (if Purpose is 'Specific')
            if (coupon.Purpose == "Specific")
            {
                var userCookie = Request.Cookies["CustomerAuth"];
                if (userCookie == null)
                {
                    return Json(new { success = false, message = "Please login to use this specific coupon." }, JsonRequestBehavior.AllowGet);
                }

                long loggedInUserId = Convert.ToInt64(userCookie["UserId"]);
                if (coupon.CustomerId != loggedInUserId)
                {
                    return Json(new { success = false, message = "This coupon is not valid for your account." }, JsonRequestBehavior.AllowGet);
                }
            }

            CouponModel showCoupenData = new CouponModel();
            showCoupenData.Purpose = coupon.Purpose;
            showCoupenData.CouponType = coupon.CouponType;
            showCoupenData.DiscountRate = coupon.DiscountRate;
            showCoupenData.DiscountPrice = coupon.DiscountPrice;
            showCoupenData.CouponCode = coupon.CouponCode;

            // All checks passed
            return Json(new
            {
                success = true,
                message = $"Coupon available!",
                CoupenData = showCoupenData
            }, JsonRequestBehavior.AllowGet);
        }
    }
}