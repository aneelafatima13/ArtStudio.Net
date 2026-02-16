using BizOne.Common;
using BizOne.Controllers;
using BizOne.DAL;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Remoting.Lifetime;
using System.Web;
using System.Web.Mvc;

namespace BizOne.Areas.Admin.Controllers
{
    public class OffDaysController : BaseController
    {
        private readonly AdminDAL dal = new AdminDAL();

        // 🔹 View
        public ActionResult OffDaysForm()
        {
            return View();
        }

        [HttpPost]
        public JsonResult SaveOffDay(OffDaysInfo model)
        {
            try
            {
                int mode = model.Id == 0 ? 1 : 2;
                string logedinid = Session["EmpId"] as string;
                model.AddedbyId = long.Parse(logedinid);
                model.ModifiedById = long.Parse(logedinid);
                if (!string.IsNullOrEmpty(model.DateRange))
                {
                    var dates = model.DateRange.Split(new[] { " to " }, StringSplitOptions.None);
                    if (dates.Length == 2)
                    {
                        model.StartDate = Convert.ToDateTime(dates[0]);
                        model.EndDate = Convert.ToDateTime(dates[1]);
                    }
                }
                var id = dal.SaveOffDay(model, mode);
                return Json(new { success = true, id });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpGet]
        public JsonResult GetOffDayById(long id)
        {
            try
            {
                var offDay = dal.GetById(id);
                offDay.AddedDateString = offDay.AddedDate.Value.ToString("dd MMM yyyy");
                offDay.ModifiedDateString = offDay.ModifiedDate.Value.ToString("dd MMM yyyy");
                string daterange = offDay.StartDate.Value.ToString("dd MMM yyyy") + " to " + offDay.EndDate.Value.ToString("dd MMM yyyy");
                offDay.DateRange = daterange;
                return Json(offDay, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        [HttpGet]
        public JsonResult GetOffDays(int page = 1, int pageSize = 10)
        {
            try
            {
                var (data, total) = dal.OffDaysList(page, pageSize);
                foreach (var leave in data)
                {
                    string daterange = leave.StartDate.Value.ToString("dd MMM yyyy") + " to " + leave.EndDate.Value.ToString("dd MMM yyyy");
                    leave.DateRange = daterange;
                }
                return Json(new { data, recordsTotal = total, recordsFiltered = total }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        [HttpPost]
        public JsonResult DeleteOffDay(long id)
        {
            try
            {
                string logedinid = Session["EmpId"] as string;
                dal.ActiveorInActiveOffDays(id, long.Parse(logedinid), 7);
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        public JsonResult ActiveorInActiveOffDays(long id, string Type)
        {
            try
            {
                string logedinid = Session["EmpId"] as string;
                if (Type == "Active")
                {
                    dal.ActiveorInActiveOffDays(id, long.Parse(logedinid), 3);
                }
                else
                {
                    dal.ActiveorInActiveOffDays(id, long.Parse(logedinid), 6);
                }
                    return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }
    }
}