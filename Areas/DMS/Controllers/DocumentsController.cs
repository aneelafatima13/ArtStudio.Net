using BizOne.Common;
using BizOne.DAL;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using static System.Net.WebRequestMethods;

namespace BizOne.Areas.DMS.Controllers
{
    public class DocumentsController : Controller
    {
        private readonly DocumentsDAL dal = new DocumentsDAL();
        private readonly AdminDAL empdal = new AdminDAL();
        public ActionResult ManageDocuments()
        {
            return View();
        }

        [HttpPost]
        public ActionResult UploadDocuments(string FolderIds, string FolderNames, long? docFolderId)
        {
            try
            {
                string userId = Session["EmpId"] as string;
                string username = Session["EmpUsername"] as string;
                string departmentName = Session["EmpDepartmentName"] as string;
               

                if (string.IsNullOrEmpty(userId))
                    return Json(new { success = false, message = "Session expired. Please login again." });

                if (Request.Files.Count == 0)
                    return Json(new { success = false, message = "No files selected." });

                // --- Root Upload Path: Department → Employee → DMSDocuments → Folder hierarchy ---
                string basePath = Server.MapPath("~/Uploads/");
                string folderPath = Path.Combine(basePath,
                                    departmentName,
                                    username,
                                    "DMSDocuments");

                // Append folder hierarchy based on breadcrumb names
                if (!string.IsNullOrEmpty(FolderNames))
                {
                    var folderParts = FolderNames.Split(',');
                    foreach (var f in folderParts)
                    {
                        folderPath = Path.Combine(folderPath, f.Trim());
                    }
                }

                if (!Directory.Exists(folderPath))
                    Directory.CreateDirectory(folderPath);
                    Directory.CreateDirectory(folderPath);

                var uploadedFiles = new List<object>();

                for (int i = 0; i < Request.Files.Count; i++)
                {
                    HttpPostedFileBase file = Request.Files[i];
                    if (file == null || file.ContentLength == 0) continue;

                    string fileName = Path.GetFileNameWithoutExtension(file.FileName);
                    string extension = Path.GetExtension(file.FileName);
                    string fullPath = Path.Combine(folderPath, file.FileName);
                    string fileType = extension?.Trim('.').ToLower(); // 👈 file type from extension

                    int counter = 1;
                    while (System.IO.File.Exists(fullPath))
                    {
                        string newFileName = $"{fileName}({counter}){extension}";
                        fullPath = Path.Combine(folderPath, newFileName);
                        counter++;
                    }

                    file.SaveAs(fullPath);

                    var fileInfo = new EmployeeFile
                    {
                        FileName = Path.GetFileName(fullPath),
                        FilePath = fullPath,
                        FileSize = (file.ContentLength / 1024) + " KB",
                        FileTitle = "DMSDocuments",
                        CreatedById = long.Parse(userId),
                        FileType = fileType,
                        FoldersOrder = FolderIds,
                        DocFolderId = docFolderId,
                    };

                    // DAL ko bhi FolderIds, FolderNames bhejna hoga
                    empdal.ManageEmployeeFile(long.Parse(userId), fileInfo, 1);
                   
                    uploadedFiles.Add(fileInfo);
                }

                return Json(new { success = true, files = uploadedFiles });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }


        [HttpGet]
        public JsonResult GetDocuments(
    long? parentId = null,
    int page = 1,
    int pageSize = 10,
    string docName = null,
    string type = null,
    DateTime? dateFrom = null,
    DateTime? dateTo = null,
    int? month = null,
    int? year = null,
    long? folderIdFilter = null
)
        {
            try
            {
                long empId = Convert.ToInt64(Session["EmpId"]);
                var (folders, files, total) = dal.GetDocuments(
                    parentId, page, pageSize, empId,
                    docName, type, dateFrom, dateTo, month, year, folderIdFilter
                );

                return Json(new { success = true, folders, files, total }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        [HttpPost]
        public JsonResult DeleteFile(long? fileId)
        {
            try
            {
                long empId = Convert.ToInt64(Session["EmpId"]);
                var Emp = new EmployeeFile { FileId = (long)fileId, EmpId = empId };
                empdal.ManageEmployeeFile(empId, Emp, 4);
                
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }


        [HttpGet]
        public ActionResult GetFile(long id, int mode)
        {
            try
            {
                var file = empdal.GetFileDatabyId(id);
                if (file == null)
                    return HttpNotFound("File not found");

                
                string contentType = GetContentType(file.FileType);
                var bytes = System.IO.File.ReadAllBytes(file.FilePath);

                if (mode == 1)
                {
                    // inline (for preview)
                    return File(bytes, contentType);
                }
                else
                {
                    // force download
                    return File(bytes, contentType, file.FileName);
                }
            }
            catch (Exception ex)
            {
                return new HttpStatusCodeResult(500, ex.Message);
            }
        }

        private string GetContentType(string ext)
        {
            switch (ext.ToLower())
            {
                case "pdf": return "application/pdf";
                case "doc": return "application/msword";
                case "docx": return "application/vnd.openxmlformats-officedocument.wordprocessingml.document";
                case "xls": return "application/vnd.ms-excel";
                case "xlsx": return "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
                case "ppt": return "application/vnd.ms-powerpoint";
                case "pptx": return "application/vnd.openxmlformats-officedocument.presentationml.presentation";
                case "txt": return "text/plain";
                case "jpg":
                case "jpeg": return "image/jpeg";
                case "png": return "image/png";
                default: return "application/octet-stream";
            }
        }

        [AllowAnonymous] 
        public ActionResult PublicFile(long id)
        {
            var file = empdal.GetFileDatabyId(id);
            if (file == null)
                return HttpNotFound();

            string contentType = GetContentType(file.FileType);
            var bytes = System.IO.File.ReadAllBytes(file.FilePath);

            return File(bytes, contentType, file.FileName);
        }

    }
}