using BizOne.Common;
using BizOne.Controllers;
using BizOne.DAL;
using System;
using System.Linq;
using System.Web.Mvc;

namespace BizOne.Areas.Products.Controllers
{
    public class ExchangeRatesController : BaseController
    {
        ProductsDAL dal = new ProductsDAL();


        [HttpPost]
        public JsonResult GetExchangeRates()
        {
            // Use the key directly instead of FirstOrDefault to avoid char conversion issues
            string draw = Request.Form["draw"];
            string startStr = Request.Form["start"];
            string lengthStr = Request.Form["length"];

            // Use TryParse for safety
            int start = string.IsNullOrEmpty(startStr) ? 0 : Convert.ToInt32(startStr);
            int length = string.IsNullOrEmpty(lengthStr) ? 10 : Convert.ToInt32(lengthStr);

            int totalRecords = 0;
            // Mode 3 = Fetch List
            var data = dal.GetExchangeRates(3, null, start, length, out totalRecords);

            return Json(new
            {
                draw = draw,
                recordsFiltered = totalRecords,
                recordsTotal = totalRecords,
                data = data
            });
        }
        [HttpGet]
        public JsonResult GetExchangeRateById(int id)
        {
            int total;
            // Mode 5 = Fetch Single by ID
            var rate = dal.GetExchangeRates(5, id, 0, 1, out total).FirstOrDefault();
            return Json(rate, JsonRequestBehavior.AllowGet);
        }
        [HttpPost]
        public JsonResult SaveExchangeRate(ExchangeRateRequest req)
        {
            try
            {
                string userId = Session["EmpId"] as string;
                if (req.mode == 1)
                {
                    req.CreatedBy = long.Parse(userId);
                }
                else
                {
                    req.ModifiedBy = long.Parse(userId);
                }

                    bool success = dal.SaveExchangeRate(req);
                return Json(new { success = success });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }
    }
}