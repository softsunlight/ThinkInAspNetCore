using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace SoftSunlight.MiniMvc.Extensions
{
    public static class StaticFileExtensions
    {
        public static WebApplication UseStaticFile(this WebApplication app)
        {
            app.Use((context, _next) =>
            {
                string webDir = AppDomain.CurrentDomain.BaseDirectory;
                string filePath = string.Empty;
                if (Regex.IsMatch(context.Request.RequstUrl, @".+\..+"))//静态文件
                {
                    filePath = Path.Combine(webDir, context.Request.RequstUrl.Remove(0, 1));
                    if (!File.Exists(filePath))
                    {
                        context.Response.StatusCode = "404";
                        context.Response.StatusMessage = "Not Found";
                    }
                    else
                    {
                        context.Response.StatusCode = "200";
                        context.Response.StatusMessage = "OK";
                        context.Response.ResponseBody = Encoding.UTF8.GetBytes(File.ReadAllText(filePath));
                    }
                    return Task.CompletedTask;
                }
                else
                {
                    return _next.Invoke();
                }

            });
            return app;
        }
    }
}
