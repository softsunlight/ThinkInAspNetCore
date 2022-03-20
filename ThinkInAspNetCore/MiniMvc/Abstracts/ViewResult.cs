using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using ThinkInAspNetCore.MiniMvc.Controllers;

namespace ThinkInAspNetCore.MiniMvc.Abstracts
{
    /// <summary>
    /// 返回html页面
    /// </summary>
    public class ViewResult : IActionResult
    {
        private Controller controller;
        private string viewName;

        public ViewResult(Controller controller, string viewName)
        {
            this.controller = controller;
            this.viewName = viewName;
            Render();
        }

        public void Render()
        {
            HttpResponse response = controller.Response;
            if (response != null)
            {
                if (response.ResponseHeaders == null)
                {
                    response.ResponseHeaders = new Dictionary<string, object>();
                }
                response.ResponseHeaders["Content-Type"] = "text/html";
                string staticHtmlDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "html");
                string fileName = string.Empty;
                if (!fileName.Contains("html"))
                {
                    fileName = viewName + ".html";
                }
                string filePath = Path.Combine(staticHtmlDir, fileName);
                if (!File.Exists(filePath))
                {
                    response.StatusCode = "404";
                    response.StatusMessage = "Not Found";
                }
                else
                {
                    response.StatusCode = "200";
                    response.StatusMessage = "OK";
                    response.ResponseBody = File.ReadAllText(filePath);
                }
            }
        }
    }
}
