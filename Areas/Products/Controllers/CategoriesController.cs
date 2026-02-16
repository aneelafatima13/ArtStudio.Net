using BizOne.Common;
using BizOne.Controllers;
using BizOne.DAL;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace BizOne.Areas.Products.Controllers
{
    public class CategoriesController : BaseController
    {
        private readonly ProductsDAL dal = new ProductsDAL();
        public ActionResult ManageCategories()
        {
            return View();
        }

        [HttpGet]
        public JsonResult GetCategories(int page = 1, int pageSize = 10, string search = null)
        {
            try
            {
                string empId = Session["EmpId"] as string;

                if (string.IsNullOrEmpty(empId))
                    throw new Exception("Session expired: EmpId not found");

                var (list, total) = dal.GetCategories(page, pageSize, search);
                return Json(new { success = true, data = list, total = total }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }


        [HttpPost]
        public JsonResult AddCategory(string name, long? parentCategoryId)
        {
            try
            {
                string empId = Session["EmpId"] as string;

                if (string.IsNullOrEmpty(empId))
                    throw new Exception("Session expired: EmpId not found");

                dal.ManageCategory(1, null, name, parentCategoryId, long.Parse(empId)); // pass current user Id instead of 1
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        public JsonResult UpdateCategory(long id, string name)
        {
            try
            {
                string empId = Session["EmpId"] as string;

                if (string.IsNullOrEmpty(empId))
                    throw new Exception("Session expired: EmpId not found");

                dal.ManageCategory(3, id, name, null, long.Parse(empId));
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        public JsonResult DeleteCategory(long id, int type)
        {
            try
            {
                string empId = Session["EmpId"] as string;

                if (string.IsNullOrEmpty(empId))
                    throw new Exception("Session expired: EmpId not found");
                if (type == 0)
                {
                    dal.ManageCategory(4, id, null, null, long.Parse(empId));
                }
                else
                {
                    dal.ManageCategory(5, id, null, null, long.Parse(empId));
                }
                    return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        public JsonResult GetCategories()
        {
            var categories = dal.CategoriesList(7); // Mode 7 = get top-level
            return Json(categories, JsonRequestBehavior.AllowGet);
        }

        // Load child categories
        [HttpPost]
        public JsonResult GetSubCategories(long parentId)
        {
            var categories = dal.CategoriesList(8, id: parentId); // Mode 8 = get children
            return Json(categories, JsonRequestBehavior.AllowGet);
        }

        }
    }