using BizOne.Common;
using BizOne.Controllers;
using BizOne.DAL;
using System;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using ActionResult = System.Web.Mvc.ActionResult;
using HttpGetAttribute = System.Web.Mvc.HttpGetAttribute;
using HttpPostAttribute = System.Web.Mvc.HttpPostAttribute;

namespace BizOne.Areas.EMS.Controllers
{
    public class EmployeesController : BaseController
    {
        private readonly AdminDAL dal = new AdminDAL();
        public ActionResult ManageEmployees()
        {
            string userId = Session["EmpId"] as string;
            string username = Session["EmpUsername"] as string;
            dal.AddActivityLog(long.Parse(userId), username + " has redirect to Manage Employees.");

            return View();
        }

        [HttpPost]
        public ActionResult ManageEmployee(Employee model)
        {
            try
            {
                long empId = 0;
                string userId = Session["EmpId"] as string;
                string username = Session["EmpUsername"] as string;
                model.CreatedById = long.Parse(userId);
                model.ModifiedById = long.Parse(userId);
                bool isuserfound = (bool)dal.ManageEmployee(model, 2);

                if (isuserfound) 
                {
                    return Json(new { success = false, message = "Username already exists" });
                } 
                else
                {
                    empId = (long)dal.ManageEmployee(model, 1);
                }

                if (model.Files != null && model.Files.Any())
                {
                    foreach (var file in model.Files)
                    {
                        string basePath = Server.MapPath("~/Uploads/");
                        string deptFolder = Path.Combine(basePath, model.DepartmentName ?? "UnknownDept");
                        string empFolder = Path.Combine(deptFolder, model.Username ?? $"Emp_{empId}");
                        string titleFolder = Path.Combine(empFolder, file.FileTitle ?? "General");

                        if (!Directory.Exists(titleFolder))
                            Directory.CreateDirectory(titleFolder);

                        string fullPath = Path.Combine(titleFolder, file.FileName);

                        System.IO.File.WriteAllBytes(fullPath, file.FileBytes);

                        file.EmpId = empId;
                        file.FilePath = fullPath;
                        file.FileSize = (file.FileBytes.Length / 1024) + " KB";
                        file.CreatedDate = DateTime.Now;
                        file.CreatedById = Convert.ToInt64(model.CreatedById);

                        dal.ManageEmployeeFile(empId, file, 1);
                        dal.AddActivityLog(long.Parse(userId), username + " has added Employee " + model.Username + " data");
                        
                    }
                }
                else
                {
                    dal.AddActivityLog(long.Parse(userId), username + " has added Employee " + model.Username + " data");

                }

                return Json(new { success = true, EmployeeId = empId });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        public ActionResult GetDepartments()
        {
            int draw = Convert.ToInt32(Request.Form["draw"]);
            int start = Convert.ToInt32(Request.Form["start"]);
            int length = Convert.ToInt32(Request.Form["length"]);

            var (departments, totalCount) = dal.GetDepartmentsWithCounts(start, length);

            return Json(new
            {
                draw = draw,
                recordsTotal = totalCount,
                recordsFiltered = totalCount,
                data = departments
            }, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public ActionResult GetEmployeesList()
        {
            int draw = Convert.ToInt32(Request.Form["draw"]);
            int start = Convert.ToInt32(Request.Form["start"]);
            int length = Convert.ToInt32(Request.Form["length"]);
            string userId = Session["EmpId"] as string;
            // Convert start/length to page number
            int pageNumber = (start / length) + 1;

            var (employees, total) = dal.GetEmployeesList(long.Parse(userId), pageNumber, length);

            return Json(new
            {
                draw = draw,
                recordsTotal = total,
                recordsFiltered = total,
                data = employees
            }, JsonRequestBehavior.AllowGet);
        }

        [HttpGet]
        public ActionResult GetEmployeeDatabyId(long empid)
        {
            Employee empdata = dal.GetEmployeeDatabyId(empid);
            return Json(new
            {
                data = empdata
            }, JsonRequestBehavior.AllowGet);

        }

        public ActionResult DownloadFile(string filePath)
        {
            if (string.IsNullOrEmpty(filePath) || !System.IO.File.Exists(filePath))
            {
                return HttpNotFound("File not found");
            }

            // Extract filename
            string fileName = Path.GetFileName(filePath);

            // Read file as bytes
            byte[] fileBytes = System.IO.File.ReadAllBytes(filePath);

            // Return as downloadable file
            return File(fileBytes, MimeMapping.GetMimeMapping(fileName), fileName);
        }

        public ActionResult GetEmployeeImage(string filePath)
        {
            return File(filePath, "image/png"); // adjust MIME type
        }

        [HttpPost]
        public ActionResult EmployeePersonalInfoUpdate(Employee model)
        {
            try
            {
                long empId = 0;
                string userId = Session["EmpId"] as string;
                string username = Session["EmpUsername"] as string;
                model.CreatedById = long.Parse(userId);
                model.ModifiedById = long.Parse(userId);
                empId = (long)dal.ManageEmployee(model, 8);

                if (model.Files != null && model.Files.Any())
                {
                    foreach (var file in model.Files)
                    {
                        string basePath = Server.MapPath("~/Uploads/");
                        string deptFolder = Path.Combine(basePath, model.DepartmentName ?? "UnknownDept");
                        string empFolder = Path.Combine(deptFolder, model.Username ?? $"Emp_{empId}");
                        string titleFolder = Path.Combine(empFolder, file.FileTitle ?? "General");

                        if (!Directory.Exists(titleFolder))
                            Directory.CreateDirectory(titleFolder);

                        // Original file path
                        string fileName = Path.GetFileNameWithoutExtension(file.FileName);
                        string extension = Path.GetExtension(file.FileName);
                        string fullPath = Path.Combine(titleFolder, file.FileName);

                        // If file already exists, add (1), (2), ... before extension
                        int counter = 1;
                        while (System.IO.File.Exists(fullPath))
                        {
                            string newFileName = $"{fileName}({counter}){extension}";
                            fullPath = Path.Combine(titleFolder, newFileName);
                            counter++;
                        }

                        // Save file
                        System.IO.File.WriteAllBytes(fullPath, file.FileBytes);

                        // Update file object with final name and path
                        file.EmpId = empId;
                        file.FileName = Path.GetFileName(fullPath); // updated file name if renamed
                        file.FilePath = fullPath;
                        file.FileSize = (file.FileBytes.Length / 1024) + " KB";
                        file.CreatedDate = DateTime.Now;
                        file.CreatedById = Convert.ToInt64(model.CreatedById);
                        file.ModifiedById = Convert.ToInt64(model.ModifiedById);

                        dal.ManageEmployeeFile(empId, file, 2);
                        dal.AddActivityLog(long.Parse(userId), username + " has updated Personal Info of Employee " + model.Username + " data");
                    }
                }
                else
                {
                    dal.AddActivityLog(long.Parse(userId), username + " has updated Personal Info of Employee " + model.Username + " data");
                }

                return Json(new { success = true, EmployeeId = empId });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        public ActionResult EmployeeContactInfoUpdate(Employee model)
        {
            try
            {
                long empId = 0;
                string userId = Session["EmpId"] as string;
                string username = Session["EmpUsername"] as string;
                model.CreatedById = long.Parse(userId);
                model.ModifiedById = long.Parse(userId);
                if (model.Type == "Edit")
                {
                    empId = (long)dal.ManageEmployee(model, 9);
                    dal.AddActivityLog(long.Parse(userId), username + " has updated Contact Info of Employee " + model.Username + " data");
                }
                else
                {
                    empId = model.Id;
                    empId = (long)dal.ManageEmployee(model, 12);
                    dal.AddActivityLog(long.Parse(userId), username + " has added 1 more row of Contact Info of Employee " + model.Username + " data");

                }

                return Json(new { success = true, EmployeeId = empId });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        public ActionResult EmployeeJobInfoUpdate(Employee model)
        {
            try
            {
                long empId = 0;
                string userId = Session["EmpId"] as string;
                string username = Session["EmpUsername"] as string;
                model.CreatedById = long.Parse(userId);
                model.ModifiedById = long.Parse(userId);
                
                if (model.Files != null && model.Files.Any())
                {
                    foreach (var file in model.Files)
                    {
                        string basePath = Server.MapPath("~/Uploads/");
                        string deptFolder = Path.Combine(basePath, model.DepartmentName ?? "UnknownDept");
                        string empFolder = Path.Combine(deptFolder, model.Username ?? $"Emp_{empId}");
                        string titleFolder = Path.Combine(empFolder, file.FileTitle ?? "General");

                        if (!Directory.Exists(titleFolder))
                            Directory.CreateDirectory(titleFolder);

                        // Original file path
                        string fileName = Path.GetFileNameWithoutExtension(file.FileName);
                        string extension = Path.GetExtension(file.FileName);
                        string fullPath = Path.Combine(titleFolder, file.FileName);

                        // If file already exists, add (1), (2), ... before extension
                        int counter = 1;
                        while (System.IO.File.Exists(fullPath))
                        {
                            string newFileName = $"{fileName}({counter}){extension}";
                            fullPath = Path.Combine(titleFolder, newFileName);
                            counter++;
                        }

                        // Save file
                        System.IO.File.WriteAllBytes(fullPath, file.FileBytes);

                        // Update file object with final name and path
                        file.EmpId = empId;
                        file.FileName = Path.GetFileName(fullPath); // updated file name if renamed
                        file.FilePath = fullPath;
                        file.FileSize = (file.FileBytes.Length / 1024) + " KB";
                        file.CreatedDate = DateTime.Now;
                        file.CreatedById = Convert.ToInt64(model.CreatedById);
                        file.ModifiedById = Convert.ToInt64(model.ModifiedById);
                        if (model.Type == "Edit")
                        {
                            dal.ManageEmployeeFile(empId, file, 2);
                            dal.AddActivityLog(long.Parse(userId), username + " has updated Job Info of Employee " + model.Username + " data");
                        }
                        else
                        {
                            dal.ManageEmployeeFile(empId, file, 1);
                            dal.AddActivityLog(long.Parse(userId), username + " has added 1 more row of Job Info of Employee " + model.Username + " data");
                        }
                    }
                }
                else
                {
                    if (model.Type == "Edit")
                    {
                        empId = (long)dal.ManageEmployee(model, 10);
                        dal.AddActivityLog(long.Parse(userId), username + " has updated Job Info of Employee " + model.Username + " data");
                    }
                    else
                    {
                        empId = (long)dal.ManageEmployee(model, 13);
                        dal.AddActivityLog(long.Parse(userId), username + " has added 1 more row of Job Info of Employee " + model.Username + " data");
                    }
                }

                return Json(new { success = true, EmployeeId = empId });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        public ActionResult EmployeeCredentialsUpdate(Employee model)
        {
            try
            {
                long empId = 0;
                string userId = Session["EmpId"] as string;
                string username = Session["EmpUsername"] as string;
                model.CreatedById = long.Parse(userId);
                model.ModifiedById = long.Parse(userId);
                if (model.Type == "Edit")
                {
                    empId = (long)dal.ManageEmployee(model, 11);
                dal.AddActivityLog(long.Parse(userId), username + " has updated Credentials Info of Employee " + model.Username + " data");
                }
                else
                {
                    empId = (long)dal.ManageEmployee(model, 14);
                    dal.AddActivityLog(long.Parse(userId), username + " has added 1 more row of Credentials Info of Employee " + model.Username + " data");
                }

                return Json(new { success = true, EmployeeId = empId });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        public ActionResult UploadFiles(Employee model)
        {
            try
            {
                long empId = model.Id;
                string userId = Session["EmpId"] as string;
                string username = Session["EmpUsername"] as string;
                model.CreatedById = long.Parse(userId);
                model.ModifiedById = long.Parse(userId);

                if (model.Files != null && model.Files.Any())
                {
                    foreach (var file in model.Files)
                    {
                        string basePath = Server.MapPath("~/Uploads/");
                        string deptFolder = Path.Combine(basePath, model.DepartmentName ?? "UnknownDept");
                        string empFolder = Path.Combine(deptFolder, model.Username ?? $"Emp_{empId}");
                        string titleFolder = Path.Combine(empFolder, file.FileTitle ?? "General");

                        if (!Directory.Exists(titleFolder))
                            Directory.CreateDirectory(titleFolder);

                        // Original file path
                        string fileName = Path.GetFileNameWithoutExtension(file.FileName);
                        string extension = Path.GetExtension(file.FileName);
                        string fullPath = Path.Combine(titleFolder, file.FileName);

                        // If file already exists, add (1), (2), ... before extension
                        int counter = 1;
                        while (System.IO.File.Exists(fullPath))
                        {
                            string newFileName = $"{fileName}({counter}){extension}";
                            fullPath = Path.Combine(titleFolder, newFileName);
                            counter++;
                        }

                        // Save file
                        System.IO.File.WriteAllBytes(fullPath, file.FileBytes);

                        // Update file object with final name and path
                        file.EmpId = model.Id;
                        file.FileName = Path.GetFileName(fullPath); // updated file name if renamed
                        file.FilePath = fullPath;
                        file.FileSize = (file.FileBytes.Length / 1024) + " KB";
                        file.CreatedDate = DateTime.Now;
                        file.CreatedById = Convert.ToInt64(model.CreatedById);
                        file.ModifiedById = Convert.ToInt64(model.ModifiedById);
                        dal.ManageEmployeeFile(empId, file, 1);
                        dal.AddActivityLog(long.Parse(userId), username + " has added more files for Employee " + model.Username + " data");

                    }
                }
               

                return Json(new { success = true, EmployeeId = empId });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        public ActionResult HandleEmployeeAction(long EmpId, string ActionType)
        {
            try
            {
                string userId = Session["EmpId"] as string;
                string username = Session["EmpUsername"] as string;
                Employee employee = new Employee() { Id = EmpId};
                if (ActionType == "inactive")
                {
                    dal.ManageEmployee(employee, 15);
                    dal.AddActivityLog(long.Parse(userId), username + " set Employee " + EmpId + " as inactive");
                }
                else if (ActionType == "delete")
                {
                    dal.ManageEmployee(employee, 16);
                    dal.AddActivityLog(long.Parse(userId), username + " deleted Employee " + EmpId + " with all related records");
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