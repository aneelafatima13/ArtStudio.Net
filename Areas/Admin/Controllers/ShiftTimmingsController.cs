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
    public class ShiftTimmingsController : BaseController
    {
        private readonly AdminDAL dal = new AdminDAL();
        public ActionResult ManageShiftTimmings()
        {
            return View();
        }
        [HttpGet]
        public JsonResult GetShifts(int draw, int start = 0, int length = 10, string search = "")
        {
            try
            {
                int page = (start / length) + 1;

                var result = dal.GetShiftList(5, page, length);
                var list = result.Shifts;
                int totalRecords = result.TotalCount;

                return Json(new
                {
                    draw = draw,
                    recordsTotal = totalRecords,
                    recordsFiltered = totalRecords, // optionally adjust if search filter is applied
                    data = list
                }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        [HttpGet]
        public JsonResult GetShiftsDropdown()
        {
            try
            {
                // Make sure the DAL method does NOT call this controller or any of its methods
                var result = dal.GetShiftList(6, null, null);
                var list = result.Shifts;
                // Return a simple, non-recursive JSON
                return Json(new { success = true, data = list }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }


        // GET: Get single record
        [HttpGet]
        public JsonResult GetShift(long id)
        {
            try
            {
                var shift = dal.GetShiftById(id);
                return Json(new { data = shift }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        // POST: Insert or Update
        [HttpPost]
        public JsonResult SaveShift(ShiftTiming model)
        {
            try
            {
                int mode = model.Id == 0 ? 1 : 2; // 1 = Insert, 2 = Update
                string userId = Session["EmpId"] as string;
                string username = Session["EmpUsername"] as string;
                model.CreatedBy = long.Parse(userId);
                model.ModifiedBy = long.Parse(userId);
                dal.InsertUpdateDeleteShift(model, mode);
                return Json(new { success = true, message = "Shift saved successfully!" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // POST: Delete
        [HttpPost]
        public JsonResult DeleteShift(long id)
        {
            try
            {
                ShiftTiming model = new ShiftTiming { Id = id };
                dal.InsertUpdateDeleteShift(model, 3); // 3 = Delete
                return Json(new { success = true, message = "Shift deleted successfully!" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }
    }
}