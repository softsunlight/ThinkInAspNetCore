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
            return View();
        }
    }
}
