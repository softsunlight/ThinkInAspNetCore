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
        //To do 页面传值

        /// <summary>
        /// http请求
        /// </summary>
        public HttpRequest Request { get; set; }
        /// <summary>
        /// http响应
        /// </summary>
        public HttpResponse Response { get; set; }
        /// <summary>
        /// 视图
        /// </summary>
        /// <returns></returns>
        public IActionResult View(string viewName)
        {
            return new ViewResult(this, viewName);
        }
        /// <summary>
        /// 返回json数据
        /// </summary>
        /// <returns></returns>
        public IActionResult Json()
        {
            return Json(null);
        }
        /// <summary>
        /// 返回json数据
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public IActionResult Json(object data)
        {
            return new JsonResult(this, data);
        }

        /// <summary>
        /// 重定向
        /// </summary>
        /// <returns></returns>
        public IActionResult Redirect()
        {
            return Redirect(null);
        }

        /// <summary>
        /// 重定向
        /// </summary>
        /// <param name="routeUrl"></param>
        /// <returns></returns>
        public IActionResult Redirect(string routeUrl)
        {
            return new RedirectResult(this, routeUrl);
        }

    }
}
