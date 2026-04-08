
using System;
using System.IO;
using System.Web.Mvc;
using FastReport;
using FastReport.Export.PdfSimple;


namespace BizOne.Areas.Reports.Controllers
{
    public class OrdersReportsController : Controller
    {
        // GET: Reports/OrdersReports
        public ActionResult OrdersReports()
        {
            return View();
        }

        [HttpGet]
        public ActionResult GetOrdersReports(
            string startDate,
            string endDate,
            int? year,
            string city,
            string country,
            string months, // Matches JavaScript 'months'
            string statuses // Matches JavaScript 'statuses'
        )
        {
            try
            {
                // 1. Initialize FastReport
                Report report = new Report();

                // 2. Set the Report File Path
                // You can make this dynamic based on a 'reportFormat' param if needed
                string reportFile = "OrdersReport.frx";
                string reportPath = Server.MapPath("~/Reports/" + reportFile);

                if (!System.IO.File.Exists(reportPath))
                {
                    return Content("Report file not found at " + reportPath);
                }

                // Register Data Connection (Required for FastReport .NET)
                FastReport.Utils.RegisteredObjects.AddConnection(typeof(FastReport.Data.MsSqlDataConnection));

                // 3. Load and Set Parameters
                report.Load(reportPath);

                // Mapping the values from your JS to the FastReport Parameters
                report.SetParameterValue("p_StartDate", startDate ?? "");
                report.SetParameterValue("p_EndDate", endDate ?? "");
                report.SetParameterValue("p_Year", year ?? DateTime.Now.Year);
                report.SetParameterValue("p_City", city ?? "");
                report.SetParameterValue("p_Country", country ?? "");
                report.SetParameterValue("p_Months", months ?? "");
                report.SetParameterValue("p_Status", statuses ?? "");

                // Add a generic title
                report.SetParameterValue("ReportTitle", "Orders Management Report");

                // 4. Prepare the Report
                report.Prepare();

                // 5. Export to PDF
                using (MemoryStream ms = new MemoryStream())
                {
                    PDFSimpleExport pdfExport = new PDFSimpleExport();
                    report.Export(pdfExport, ms);
                    ms.Position = 0;

                    // Set headers for inline viewing in the iframe
                    var cd = new System.Net.Mime.ContentDisposition
                    {
                        FileName = "Orders_Report_" + DateTime.Now.ToString("yyyyMMdd") + ".pdf",
                        Inline = true,
                    };

                    Response.AppendHeader("Content-Disposition", cd.ToString());
                    return File(ms.ToArray(), "application/pdf");
                }
            }
            catch (Exception ex)
            {
                // Return the error message so you can see it in the iframe if it fails
                return Content("Error generating report: " + ex.Message);
            }
        }
    }
}