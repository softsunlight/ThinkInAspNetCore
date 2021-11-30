using System;
using System.Collections.Generic;
using System.Text;
using ThinkInAspNetCore.MiniMvc.Abstracts;

namespace ThinkInAspNetCore.MiniMvc.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            //HttpSession httpSession = new HttpSession(Request, Response);
            //if (httpSession["user"] == null)
            //{
            //    return Redirect("/Home/Login");
            //}
            return View("Index");
        }

        public IActionResult Test()
        {
            return Json(new
            {
                Success = true,
                Msg = ""
            });
        }

        public IActionResult Login()
        {
            return View("Login");
        }

        public IActionResult LoginVerify()
        {
            if (Request.Form != null && Request.Form.Count > 0)
            {
                if (Request.Form.ContainsKey("username"))
                {
                    HttpSession httpSession = new HttpSession(Request, Response);
                    httpSession["user"] = Request.Form["username"];
                }
            }
            else if (Request.QueryString != null && Request.QueryString.Count > 0)
            {
                if (Request.QueryString.ContainsKey("username"))
                {
                    if (Request.QueryString.ContainsKey("username"))
                    {
                        HttpSession httpSession = new HttpSession(Request, Response);
                        httpSession["user"] = Request.QueryString["username"];
                    }
                }
            }
            return Redirect("/");
        }

        public IActionResult File()
        {
            return View("File");
        }

    }
}
