using System;
using System.Collections.Generic;
using System.IO;
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

                if (httpRequest.Files != null && httpRequest.Files.Count > 0)
                {
                    foreach (var file in httpRequest.Files)
                    {
                        file.Write(Path.Combine(webDir, file.FileName));
                    }
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
                    if (httpRequest.RequstUrl == "/")
                    {
                        HttpSession httpSession = new HttpSession(httpRequest, httpResponse);
                        if (httpSession["user"] == null)
                        {
                            httpResponse.StatusCode = "302";
                            httpResponse.StatusMessage = "Found";
                            if (httpResponse.ResponseHeaders == null)
                            {
                                httpResponse.ResponseHeaders = new Dictionary<string, object>();
                            }
                            httpResponse.ResponseHeaders.Add("Location", "html/login.html");
                        }
                        else
                        {
                            filePath = Path.Combine(webDir, "html/index.html");
                            httpResponse.ContentType = "text/html";
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
                    }
                    else if (httpRequest.RequstUrl.Equals("/LoginVerify", StringComparison.InvariantCultureIgnoreCase))
                    {
                        //登录验证
                        if (httpRequest.Form != null && httpRequest.Form.Count > 0)
                        {
                            if (httpRequest.Form.ContainsKey("username"))
                            {
                                HttpSession httpSession = new HttpSession(httpRequest, httpResponse);
                                httpSession["user"] = httpRequest.Form["username"];
                            }
                        }
                        else if (httpRequest.QueryString != null && httpRequest.QueryString.Count > 0)
                        {
                            if (httpRequest.QueryString.ContainsKey("username"))
                            {
                                if (httpRequest.QueryString.ContainsKey("username"))
                                {
                                    HttpSession httpSession = new HttpSession(httpRequest, httpResponse);
                                    httpSession["user"] = httpRequest.QueryString["username"];
                                }
                            }
                        }
                        //重定向
                        httpResponse.StatusCode = "302";
                        httpResponse.StatusMessage = "Found";
                        if (httpResponse.ResponseHeaders == null)
                        {
                            httpResponse.ResponseHeaders = new Dictionary<string, object>();
                        }
                        httpResponse.ResponseHeaders.Add("Location", "/");
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
                throw ex;
            }
            finally
            {
                httpResponse.Write();
            }
        }

    }
}
