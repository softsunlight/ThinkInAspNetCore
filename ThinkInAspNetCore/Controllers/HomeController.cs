using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using SoftSunlight.MiniMvc.Abstracts;

namespace SoftSunlight.MiniMvc.Controllers
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

        public IActionResult SaveFile()
        {
            if (Request.Files != null && Request.Files.Count > 0)
            {
                foreach (var file in Request.Files)
                {
                    file.Write(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, file.FileName));
                }
            }
            return Json(new
            {
                Success = true
            });
        }

        public IActionResult Form()
        {
            return View("Form");
        }

        public IActionResult WebSocketTest()
        {
            return View("WebSocketTest");
        }

        public IActionResult Ajax()
        {
            return View("ajax");
        }

    }
}
