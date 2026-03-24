using BizOne.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace BizOne.Models
{
    public static class LoginUser
    {
        public static int EmpId
        {
            get { return Convert.ToInt32(HttpContext.Current.Session["EmpId"]); }
            set { HttpContext.Current.Session["EmpId"] = value; }
        }

        public static string EmpUsername
        {
            get { return HttpContext.Current.Session["EmpUsername"] as string; }
            set { HttpContext.Current.Session["EmpUsername"] = value; }
        }

        public static string EmpRole
        {
            get { return HttpContext.Current.Session["EmpRole"] as string; }
            set { HttpContext.Current.Session["EmpRole"] = value; }
        }

        public static Employee UserData
        {
            get { return HttpContext.Current.Session["UserData"] as Employee; }
            set { HttpContext.Current.Session["UserData"] = value; }
        }

        public static List<string> EmpRightsList
        {
            get { return HttpContext.Current.Session["EmpRightsList"] as List<string>; }
            set { HttpContext.Current.Session["EmpRightsList"] = value; }
        }

        public static bool IsLoggedIn => HttpContext.Current.Session["EmpId"] != null;

        public static RightsFlagsList Rights =>
    HttpContext.Current.Session["RightsFlags"] as RightsFlagsList;


    }

}