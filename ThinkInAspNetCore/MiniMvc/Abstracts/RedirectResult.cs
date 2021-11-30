using System;
using System.Collections.Generic;
using System.Text;
using ThinkInAspNetCore.MiniMvc.Controllers;

namespace ThinkInAspNetCore.MiniMvc.Abstracts
{
    /// <summary>
    /// 重定向
    /// </summary>
    public class RedirectResult : IActionResult
    {
        private Controller controller;

        private string routeUrl;

        public RedirectResult(Controller controller, string routeUrl)
        {
            this.controller = controller;
            this.routeUrl = routeUrl;
            Render();
        }

        public void Render()
        {
            HttpResponse response = controller.Response;
            if (response != null)
            {
                response.StatusCode = "302";
                response.StatusMessage = "Found";
                if (response.ResponseHeaders == null)
                {
                    response.ResponseHeaders = new Dictionary<string, object>();
                }
                response.ResponseHeaders.Add("Location", string.IsNullOrEmpty(routeUrl) ? "/" : routeUrl);
            }
        }
    }
}
