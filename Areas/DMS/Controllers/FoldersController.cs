using BizOne.Common;
using BizOne.Controllers;
using BizOne.DAL;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Web;
using System.Web.Mvc;
using System.Xml.Linq;

namespace BizOne.Areas.DMS.Controllers
{
    public class FoldersController : BaseController
    {
        private readonly DocumentsDAL dal = new DocumentsDAL();
        private readonly AdminDAL empdal = new AdminDAL();

        [HttpPost]
        public JsonResult AddFolder(EmpFolder model)
        {
            try
            {
                model.AddedById = Convert.ToInt64(Session["EmpId"]);
                long folderfound = dal.ManageEmpFolder(model, 6);
                if (folderfound == 0)
                {
                    long newId = dal.ManageEmpFolder(model, 1); // mode = 1 for insert

                    return Json(new { success = true, folderId = newId });
                }
                else
                {
                    return Json(new { success = false, message = "A Folder with same name already exists! please change folder name" });
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpGet]
        public JsonResult GetAllFolders()
        {
            long empId = Convert.ToInt64(Session["EmpId"]);
            var folders = dal.GetFolders(7, empId);
            return Json(new { success = true, folders = folders }, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public JsonResult UpdateFolderName(long id, string name)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(name))
                {
                    return Json(new { success = false, message = "Folder name cannot be empty." });
                }

                EmpFolder modal = new EmpFolder()
                {
                    Id = id,
                    FolderName = name
                };

                dal.ManageEmpFolder(modal, 2);

                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }


        [HttpPost]
        public JsonResult DeleteFolder(long id)
        {
            try
            {
                EmpFolder modal = new EmpFolder()
                {
                    Id = id
                };

                dal.ManageEmpFolder(modal, 3);

                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }


        [HttpPost]
        public JsonResult MoveorDelete(long sourceId, long targetId, long? fileId, int mode)
        {
            try
            {
                long empId = Convert.ToInt64(Session["EmpId"]);
                long? movedId = null;
                if (fileId == null)
                {
                    var folder = new EmpFolder { Id = sourceId, AddedById = empId, ParentFolderId = targetId };
                     movedId = dal.ManageEmpFolder(folder, mode);
                }
                else
                {
                    var Emp = new EmployeeFile { FileId = (long)fileId, EmpId = empId, DocFolderId = targetId };
                    empdal.ManageEmployeeFile(empId, Emp, 3);
                }


                    return Json(new { success = true, folderId = movedId });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

    }



}
