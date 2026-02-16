using BizOne.Controllers;
using BizOne.DAL;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace BizOne.Areas.EMS.Controllers
{
    public class AttendanceController : BaseController
    {
        private readonly AdminDAL dal = new AdminDAL();

        public ActionResult AttendanceSheet()
        {
            return View();
        }

        [HttpGet]
        public JsonResult GetList(DateTime? dateFrom, DateTime? dateTo, string day, string attendance,
                  string leaveType, string offType, string status,
                  string time, int? month, int? year)
        {
            try
            {
                string empId = Session["EmpId"] as string;

                if (string.IsNullOrEmpty(empId))
                    throw new Exception("Session expired: EmpId not found");

                TimeSpan? timeFilter = null;
                if (!string.IsNullOrEmpty(time))
                    timeFilter = TimeSpan.Parse(time);

                var list = dal.GetAttendanceList(1, long.Parse(empId), month, year,
                                                 dateFrom, dateTo, day, attendance,
                                                 leaveType, offType, status, timeFilter);

                foreach (var l in list)
                {
                    // Date
                    if (l.Date != null && DateTime.TryParse(l.Date.ToString(), out DateTime parsedDate))
                        l.DateString = parsedDate.ToString("dd MMM yyyy");
                    else
                        l.DateString = string.Empty;

                    // CheckIn
                    if (l.CheckIn != null && DateTime.TryParse(l.CheckIn.ToString(), out DateTime parsedCheckIn))
                        l.CheckInString = parsedCheckIn.ToString("hh:mm:ss tt");
                    else
                        l.CheckInString = string.Empty;

                    // CheckOut
                    if (l.CheckOut != null && DateTime.TryParse(l.CheckOut.ToString(), out DateTime parsedCheckOut))
                        l.CheckOutString = parsedCheckOut.ToString("hh:mm:ss tt");
                    else
                        l.CheckOutString = string.Empty;
                }

                return Json(new { success = true, data = list }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                // Always return JSON on error (instead of 500)
                return Json(new { success = false, message = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        public void APICallcheckingLeavesorOFFs()
        {
            dal.GetAttendanceList(2, null, null, null);
        }

        public void APICallmarkingabsent()
        {
            dal.GetAttendanceList(3, null, null, null);
        }

        [HttpPost]
        public JsonResult CheckIn(long id)
        {
            try
            {
                dal.GetAttendanceList(4, id); 
                return Json(new { success = true, message = "CheckIn marked successfully" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // ✅ CheckOut
        [HttpPost]
        public JsonResult CheckOut(long id)
        {
            try
            {
                dal.GetAttendanceList(5, id); 
                return Json(new { success = true, message = "CheckOut marked successfully" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }
    }
}

