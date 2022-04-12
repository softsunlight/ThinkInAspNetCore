using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace ThinkInAspNetCore.MiniMvc.Extensions
{
    public static class CORSExtensions
    {
        public static WebApplication UseCors(this WebApplication webApplication)
        {
            webApplication.Use((context, _next) =>
            {
                if (context.Request.Method == "OPTIONS")
                {
                    if (context.Request.RequestHeaders.ContainsKey("Access-Control-Request-Method"))
                    {
                        //CORS 预检请求
                        if (context.Request.RequestHeaders.ContainsKey("Origin"))
                        {
                            if (context.Response.ResponseHeaders == null)
                            {
                                context.Response.ResponseHeaders = new Dictionary<string, object>();
                            }
                            context.Response.ResponseHeaders["Access-Control-Allow-Origin"] = context.Request.RequestHeaders["Origin"];
                        }
                        context.Response.ResponseHeaders["Access-Control-Allow-Methods"] = "POST,GET,OPTIONS";
                        if (context.Request.RequestHeaders.ContainsKey("Access-Control-Request-Headers"))
                        {
                            context.Response.ResponseHeaders["Access-Control-Allow-Headers"] = context.Request.RequestHeaders["Access-Control-Request-Headers"];
                        }
                        context.Response.StatusCode = "204";
                        context.Response.StatusMessage = "No Content";
                    }
                    return Task.CompletedTask;
                }
                else
                {
                    return _next.Invoke();
                }
            });
            return webApplication;
        }
    }
}
