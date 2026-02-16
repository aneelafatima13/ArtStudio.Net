using BizOne.Common;
using BizOne.Controllers;
using BizOne.DAL;
using System;
using System.Web.Mvc;

namespace BizOne.Areas.EMS.Controllers
{
    public class LeavesController : BaseController
    {
        private readonly AdminDAL dal = new AdminDAL();

        // 🔹 View
        public ActionResult LeaveForm()
        {
            return View();
        }

        // 🔹 Save (Insert/Update) leave
        [HttpPost]
        public JsonResult SaveLeave(LeavesInfo leave)
        {
            try
            {
                // extract start/end from DateRange (format: "yyyy-MM-dd to yyyy-MM-dd")
                if (!string.IsNullOrEmpty(leave.DateRange))
                {
                    var dates = leave.DateRange.Split(new[] { " to " }, StringSplitOptions.None);
                    if (dates.Length == 2)
                    {
                        leave.StartDate = Convert.ToDateTime(dates[0]);
                        leave.EndDate = Convert.ToDateTime(dates[1]);
                    }
                }

                int mode = leave.Id == 0 ? 1 : 2; // 1 = Insert, 2 = Update
                long newId = dal.ManageLeave(leave, mode);

                return Json(new { success = true, id = newId });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // 🔹 Get leave by Id
        [HttpGet]
        public JsonResult GetLeaveById(long id)
        {
            try
            {
                var leave = dal.GetLeaveById(id);
                leave.ApplyDateString = leave.ApplyDate.Value.ToString("dd MMM yyyy");
                leave.ApprovedDateString = leave.ApprovedDate.Value.ToString("dd MMM yyyy");
                string daterange = leave.StartDate.Value.ToString("dd MMM yyyy") + " to " + leave.EndDate.Value.ToString("dd MMM yyyy");
                leave.DateRange = daterange;
                return Json(leave, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        // 🔹 Get all leaves for employee (with pagination)
        [HttpGet]
        public JsonResult GetLeavesByEmployee(long empId, int pageNumber = 1, int pageSize = 10)
        {
            try
            {
                var result = dal.GetLeaves(empId, pageNumber, pageSize, 4);
                var leaves = result.Data;
                var totalRecords = result.TotalRecords;

                foreach (var leave in leaves)
                {
                    string daterange = leave.StartDate.Value.ToString("dd MMM yyyy") + " to " + leave.EndDate.Value.ToString("dd MMM yyyy");
                    leave.DateRange = daterange;
                }
                return Json(new { data = leaves, recordsTotal = totalRecords, recordsFiltered = totalRecords }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        [HttpPost]
        public JsonResult DeleteLeave(long id)
        {
            try
            {
                LeavesInfo leave = dal.GetLeaveById(id);
                
                dal.ManageLeave(new LeavesInfo { Id = id }, 3); // mode 3 = Delete
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }


        public ActionResult ApproveLeaves()
        {
            return View();
        }


        [HttpGet]
        public JsonResult GetLeavesList(int pageNumber = 1, int pageSize = 10)
        {
            try
            {
                var result = dal.GetLeaves(null, pageNumber, pageSize, 4);
                var leaves = result.Data;
                var totalRecords = result.TotalRecords;

                foreach (var leave in leaves)
                {
                    string daterange = leave.StartDate.Value.ToString("dd MMM yyyy") + " to " + leave.EndDate.Value.ToString("dd MMM yyyy");
                    leave.DateRange = daterange;
                }
                return Json(new { data = leaves, recordsTotal = totalRecords, recordsFiltered = totalRecords }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        [HttpPost]
        public JsonResult ApproveLeave(long id,string type)
        {
            try
            {
                LeavesInfo leave = dal.GetLeaveById(id);
                if (type == "approve")
                {
                    dal.ManageLeave(new LeavesInfo { Id = id }, 6);
                }
                else
                {
                    dal.ManageLeave(new LeavesInfo { Id = id }, 8);
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
