using System;
using System.Collections.Generic;
using System.Text;
using ThinkInAspNetCore.MiniMvc.Abstracts;

namespace ThinkInAspNetCore.MiniMvc.Controllers
{
    /// <summary>
    /// 控制器
    /// </summary>
    public abstract class Controller
    {
        public IActionResult View()
        {
            return new ViewResult();
        }
    }
}
