using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace ThinkInAspNetCore.MiniMvc
{
    /// <summary>
    /// http请求处理类
    /// </summary>
    public class HttpHandler
    {
        public HttpRequest httpRequest { get; set; }
        public HttpResponse httpResponse { get; set; }

        private static Dictionary<string, Type> controller2Type;
        private static Dictionary<string, Dictionary<string, MethodInfo>> controllerAction2Method;

        static HttpHandler()
        {
            controller2Type = new Dictionary<string, Type>();
            controllerAction2Method = new Dictionary<string, Dictionary<string, MethodInfo>>();
            Type[] allTypes = Assembly.GetExecutingAssembly().GetTypes();
            foreach (var tempType in allTypes)
            {
                if (!tempType.IsAbstract && Regex.IsMatch(tempType.FullName, @"(?is).+Controller$"))
                {
                    string name = tempType.Name.Replace("Controller", "").ToLower();
                    controller2Type[name] = tempType;
                    controllerAction2Method[name] = new Dictionary<string, MethodInfo>();
                    foreach (var mi in tempType.GetMethods())
                    {
                        if (mi.IsPublic && !mi.IsStatic && !mi.IsConstructor)
                        {
                            controllerAction2Method[name][mi.Name.ToLower()] = mi;
                        }
                    }
                }
            }
        }

        public HttpHandler()
        {

        }

        public HttpHandler(HttpRequest httpRequest) : this()
        {
            this.httpRequest = httpRequest;
            this.httpResponse = new HttpResponse();
        }

        /// <summary>
        /// 处理请求
        /// </summary>
        public void ProcessRequest()
        {
            try
            {
                httpResponse.Version = httpRequest == null ? "HTTP/1.1" : httpRequest.Version;
                httpResponse.ResponseStream = httpRequest.RequestStream;
                string webDir = AppDomain.CurrentDomain.BaseDirectory;
                string filePath = string.Empty;

                if (httpRequest.Method == "OPTIONS")
                {
                    if (httpRequest.RequestHeaders.ContainsKey("Access-Control-Request-Method"))
                    {
                        //CORS 预检请求
                        if (httpRequest.RequestHeaders.ContainsKey("Origin"))
                        {
                            if (httpResponse.ResponseHeaders == null)
                            {
                                httpResponse.ResponseHeaders = new Dictionary<string, object>();
                            }
                            httpResponse.ResponseHeaders["Access-Control-Allow-Origin"] = httpRequest.RequestHeaders["Origin"];
                        }
                        httpResponse.ResponseHeaders["Access-Control-Allow-Methods"] = "POST,GET,OPTIONS";
                        if (httpRequest.RequestHeaders.ContainsKey("Access-Control-Request-Headers"))
                        {
                            httpResponse.ResponseHeaders["Access-Control-Allow-Headers"] = httpRequest.RequestHeaders["Access-Control-Request-Headers"];
                        }
                        httpResponse.StatusCode = "204";
                        httpResponse.StatusMessage = "No Content";
                        return;
                    }
                }

                //web socket
                if (httpRequest.RequestHeaders.ContainsKey("Upgrade") && httpRequest.RequestHeaders["Upgrade"].Equals("websocket"))
                {
                    //
                    var buffer = Encoding.UTF8.GetBytes(httpRequest.RequestHeaders["Sec-WebSocket-Key"].ToString() + "258EAFA5-E914-47DA-95CA-C5AB0DC85B11");
                    var data = SHA1.Create().ComputeHash(buffer);
                    httpResponse.StatusCode = "101";
                    httpResponse.StatusMessage = "Switching Protocols";
                    if (httpResponse.ResponseHeaders == null)
                    {
                        httpResponse.ResponseHeaders = new Dictionary<string, object>();
                    }
                    httpResponse.ResponseHeaders.Add("Connection", "Upgrade");
                    httpResponse.ResponseHeaders.Add("Upgrade", "websocket");
                    httpResponse.ResponseHeaders.Add("Sec-WebSocket-Accept", Convert.ToBase64String(data));
                    return;
                }

                if (Regex.IsMatch(httpRequest.RequstUrl, @".+\..+"))//静态文件
                {
                    filePath = Path.Combine(webDir, httpRequest.RequstUrl.Remove(0, 1));
                    if (!File.Exists(filePath))
                    {
                        httpResponse.StatusCode = "404";
                        httpResponse.StatusMessage = "Not Found";
                    }
                    else
                    {
                        httpResponse.StatusCode = "200";
                        httpResponse.StatusMessage = "OK";
                        httpResponse.ResponseBody = File.ReadAllText(filePath);
                    }
                }
                else
                {
                    string requestUrl = httpRequest.RequstUrl.ToLower();
                    string[] routeArr = requestUrl.Split("/", StringSplitOptions.RemoveEmptyEntries);
                    string controllerName = string.Empty;
                    string actionName = string.Empty;
                    if (routeArr.Length >= 2)
                    {
                        controllerName = routeArr[0].Trim();
                        actionName = routeArr[1].Trim();
                    }
                    if (string.IsNullOrEmpty(controllerName))
                    {
                        controllerName = "home";
                    }
                    if (string.IsNullOrEmpty(actionName))
                    {
                        actionName = "index";
                    }

                    Type type = null;
                    if (controller2Type.ContainsKey(controllerName))
                    {
                        type = controller2Type[controllerName];
                    }
                    if (type != null)
                    {
                        var controllerObj = Activator.CreateInstance(type);
                        type.GetProperty("Request").SetValue(controllerObj, httpRequest);
                        type.GetProperty("Response").SetValue(controllerObj, httpResponse);
                        MethodInfo actionMem = null;
                        if (controllerAction2Method[controllerName].ContainsKey(actionName))
                        {
                            actionMem = controllerAction2Method[controllerName][actionName];
                        }
                        if (actionMem != null)
                        {
                            actionMem.Invoke(controllerObj, null);
                        }
                        else
                        {
                            httpResponse.StatusCode = "404";
                            httpResponse.StatusMessage = "Not Found";
                        }
                    }
                    else
                    {
                        httpResponse.StatusCode = "404";
                        httpResponse.StatusMessage = "Not Found";
                    }
                }
            }
            catch (Exception ex)
            {
                httpResponse.ContentType = "text/html";
                httpResponse.ResponseBody = File.ReadAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "html/error_500.html")).Replace("@error", "处理您的请求出错了，" + ex.Message + "," + ex.Source + "," + ex.StackTrace);
                httpResponse.StatusCode = "500";
                httpResponse.StatusMessage = "Error";
                if (httpResponse.ResponseHeaders == null)
                {
                    httpResponse.ResponseHeaders = new Dictionary<string, object>();
                }
                httpResponse.ResponseHeaders["Server"] = "softsunlight_webserver";
                httpResponse.ResponseHeaders["Date"] = DateTime.Now.ToLongTimeString();
                //throw ex;
            }
            finally
            {
                httpResponse.Write();
            }
        }

    }
}
