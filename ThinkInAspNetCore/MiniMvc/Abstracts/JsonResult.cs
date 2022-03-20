using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using ThinkInAspNetCore.MiniMvc.Controllers;

namespace ThinkInAspNetCore.MiniMvc.Abstracts
{
    /// <summary>
    /// 返回json数据
    /// </summary>
    public class JsonResult : IActionResult
    {
        private Controller controller;

        private object jsonData;

        public JsonResult(Controller controller, object data)
        {
            this.controller = controller;
            this.jsonData = data;
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
                response.ResponseHeaders["Content-Type"] = "application/json;charset=utf-8";
                if (jsonData == null)
                {
                    response.ResponseBody = "{}";
                }
                else
                {
                    response.ResponseBody = JsonSerializer.Serialize(jsonData);
                }
                response.StatusCode = "200";
                response.StatusMessage = "OK";
            }
        }
    }
}
