using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace SoftSunlight.MiniMvc.Extensions
{
    public static class WebSocketExtensions
    {
        public static WebApplication UseWebSocket(this WebApplication webApplication)
        {
            webApplication.Use((context, _next) =>
            {
                //web socket
                if (context.Request.RequestHeaders != null && context.Request.RequestHeaders.ContainsKey("Upgrade") && context.Request.RequestHeaders["Upgrade"].Equals("websocket"))
                {
                    //
                    var buffer = Encoding.UTF8.GetBytes(context.Request.RequestHeaders["Sec-WebSocket-Key"].ToString() + "258EAFA5-E914-47DA-95CA-C5AB0DC85B11");
                    var data = SHA1.Create().ComputeHash(buffer);
                    context.Response.StatusCode = "101";
                    context.Response.StatusMessage = "Switching Protocols";
                    if (context.Response.ResponseHeaders == null)
                    {
                        context.Response.ResponseHeaders = new Dictionary<string, object>();
                    }
                    context.Response.ResponseHeaders.Add("Connection", "Upgrade");
                    context.Response.ResponseHeaders.Add("Upgrade", "websocket");
                    context.Response.ResponseHeaders.Add("Sec-WebSocket-Accept", Convert.ToBase64String(data));
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
