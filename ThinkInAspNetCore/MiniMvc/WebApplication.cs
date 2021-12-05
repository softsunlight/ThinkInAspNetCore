using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace ThinkInAspNetCore.MiniMvc
{
    public class WebApplication
    {
        private string[] args;

        public WebApplication()
        {

        }

        public WebApplication(string[] args)
        {
            this.args = args;
        }

        /// <summary>
        /// 启动web应用
        /// </summary>
        public void Run()
        {
            try
            {
                //to do 根据args参数来监听端口和IP
                TcpListener tcpListener = new TcpListener(IPAddress.Parse("127.0.0.1"), 5000);
                tcpListener.Start();
                while (true)
                {
                    TcpClient tcpClient = tcpListener.AcceptTcpClient();
                    Task.Run(() =>
                    {
                        NetworkStream networkStream = null;
                        HttpHandler httpHandler = null;
                        HttpRequest httpRequest = new HttpRequest();
                        try
                        {
                            networkStream = tcpClient.GetStream();
                            httpRequest.RequestStream = networkStream;
                            MemoryStream memoryStream = new MemoryStream();
                            int recvTotals = 0;
                            while (networkStream.DataAvailable)
                            {
                                byte[] buffer = new byte[512];
                                int realLength = networkStream.Read(buffer, 0, buffer.Length);
                                memoryStream.Write(buffer, 0, realLength);
                                recvTotals += realLength;
                            }
                            if (recvTotals > 0)
                            {
                                httpRequest = BuildRequest(tcpClient, memoryStream.ToArray());
                                string content = Encoding.UTF8.GetString(memoryStream.ToArray());
                                Log.Write(content);
                                if (memoryStream != null)
                                {
                                    memoryStream.Close();
                                    memoryStream.Dispose();
                                    memoryStream = null;
                                }
                                httpHandler = new HttpHandler(httpRequest);
                            }
                            else
                            {
                                if (networkStream != null)
                                {
                                    networkStream.Close();
                                    networkStream.Dispose();
                                    networkStream = null;
                                }
                                if (tcpClient != null)
                                {
                                    tcpClient.Close();
                                    tcpClient.Dispose();
                                    tcpClient = null;
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            Log.Write("处理请求出错", ex);
                            if (httpHandler == null)
                            {
                                httpHandler = new HttpHandler();
                                if (httpHandler.httpResponse == null)
                                {
                                    httpHandler.httpResponse = new HttpResponse();
                                    httpHandler.httpResponse.ResponseStream = tcpClient.GetStream();
                                    httpHandler.httpResponse.ContentType = "text/html";
                                    httpHandler.httpResponse.ResponseBody = File.ReadAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "html/error_500.html")).Replace("@error", "处理您的请求出错了，" + ex.Message + "," + ex.Source + "," + ex.StackTrace);
                                    httpHandler.httpResponse.StatusCode = "500";
                                    httpHandler.httpResponse.StatusMessage = "Error";
                                    if (httpHandler.httpResponse.ResponseHeaders == null)
                                    {
                                        httpHandler.httpResponse.ResponseHeaders = new Dictionary<string, object>();
                                    }
                                    httpHandler.httpResponse.ResponseHeaders["Server"] = "softsunlight_webserver";
                                    httpHandler.httpResponse.ResponseHeaders["Date"] = DateTime.Now.ToLongTimeString();
                                }
                            }
                        }
                        finally
                        {
                            if (httpHandler != null)
                            {
                                httpHandler.ProcessRequest();
                            }
                            //if (networkStream != null)
                            //{
                            //    networkStream.Close();
                            //    networkStream.Dispose();
                            //    networkStream = null;
                            //}
                            //if (tcpClient != null)
                            //{
                            //    tcpClient.Close();
                            //    tcpClient.Dispose();
                            //    tcpClient = null;
                            //}
                        }
                    });
                }
            }
            catch (Exception ex)
            {
                Log.Write("启动web服务出错", ex);
                Thread.Sleep(10000);
                Run();
            }
        }

        /// <summary>
        /// 构造Http请求类
        /// </summary>
        /// <param name="tcpClient"></param>
        /// <param name="requestDatas"></param>
        /// <returns></returns>
        private static HttpRequest BuildRequest(TcpClient tcpClient, byte[] requestDatas)
        {
            HttpRequest httpRequest = new HttpRequest();
            if (tcpClient.Connected)
            {
                httpRequest.RequestStream = tcpClient.GetStream();
            }
            if (requestDatas.Length <= 0)
            {
                throw new Exception("请求内容为空");
            }
            int formContentIndex = int.MaxValue;
            int lastSpaceCharIndex = 0;
            string name = string.Empty;
            string fileName = string.Empty;
            string contentType = string.Empty;
            bool isValue = false;
            string boundary = string.Empty;
            List<byte> fileDataList = new List<byte>();
            for (int i = 0; i < requestDatas.Length; i++)
            {
                if ((requestDatas[i] == 13 && requestDatas[i + 1] == 10) || i == requestDatas.Length - 1)
                {
                    //遇到空行，则下一行是表单域
                    if (i - lastSpaceCharIndex == 0)
                    {
                        if (formContentIndex == int.MaxValue)
                        {
                            formContentIndex = i + 2;
                            lastSpaceCharIndex = formContentIndex;
                        }
                        if (i >= formContentIndex)
                        {
                            lastSpaceCharIndex = i + 2;
                            isValue = true;
                        }
                        continue;
                    }
                    if (i <= lastSpaceCharIndex)
                    {
                        continue;
                    }
                    if (i >= formContentIndex)
                    {
                        if (httpRequest.RequestHeaders.ContainsKey("Content-Type") && httpRequest.RequestHeaders["Content-Type"].ToString().Contains("multipart/form-data"))
                        {
                            //构造文件列表
                            if (string.IsNullOrEmpty(boundary))
                            {
                                Regex boundaryRegex = new Regex(@"boundary=(?<boundary>\S+)");
                                Match boundaryMatch = boundaryRegex.Match(httpRequest.RequestHeaders["Content-Type"].ToString());
                                if (boundaryMatch.Success)
                                {
                                    boundary = boundaryMatch.Groups["boundary"].Value;
                                }
                            }
                            string tempStr = Encoding.UTF8.GetString(requestDatas, lastSpaceCharIndex, i == requestDatas.Length ? i - lastSpaceCharIndex + 1 : i - lastSpaceCharIndex);
                            if (tempStr.Contains(boundary))
                            {
                                lastSpaceCharIndex = i + 2;
                                if (fileDataList.Count > 0)
                                {
                                    if (httpRequest.Files == null)
                                    {
                                        httpRequest.Files = new List<HttpFile>();
                                    }
                                    httpRequest.Files.Add(new HttpFile()
                                    {
                                        Name = name,
                                        FileName = fileName,
                                        FileDatas = fileDataList.ToArray(),
                                        ContentType = contentType
                                    });
                                    fileDataList = new List<byte>();
                                }
                                if (!string.IsNullOrEmpty(name))
                                {
                                    name = string.Empty;
                                }
                                if (!string.IsNullOrEmpty(fileName))
                                {
                                    fileName = string.Empty;
                                }
                                if (!string.IsNullOrEmpty(contentType))
                                {
                                    contentType = string.Empty;
                                }
                                continue;
                            }
                            Regex nameReg = new Regex(@"name=""(?<name>[^""]*)""");
                            Regex filenameReg = new Regex(@"filename=""(?<filename>[^""]*)""");
                            Regex contentTypeReg = new Regex(@"Content-Type\s*:\s*(?<contentType>\S*)");
                            Match nameMatch = nameReg.Match(tempStr);
                            if (nameMatch.Success)
                            {
                                name = nameMatch.Groups["name"].Value;
                            }
                            Match filenameMatch = filenameReg.Match(tempStr);
                            if (filenameMatch.Success)
                            {
                                fileName = filenameMatch.Groups["filename"].Value;
                            }
                            Match contentTypeMatch = contentTypeReg.Match(tempStr);
                            if (contentTypeMatch.Success)
                            {
                                contentType = contentTypeMatch.Groups["contentType"].Value;
                            }
                            if (isValue)
                            {
                                if (string.IsNullOrEmpty(fileName))
                                {
                                    if (!string.IsNullOrEmpty(name))
                                    {
                                        if (httpRequest.Form == null)
                                        {
                                            httpRequest.Form = new Dictionary<string, string>();
                                        }
                                        httpRequest.Form[name] = tempStr;
                                    }
                                    isValue = false;
                                }
                                else
                                {
                                    byte[] fileDatas = new byte[i - lastSpaceCharIndex];
                                    Array.Copy(requestDatas, lastSpaceCharIndex, fileDatas, 0, i - lastSpaceCharIndex);
                                    fileDataList.AddRange(fileDatas);
                                }
                            }
                        }
                        else
                        {
                            string tempStr = Encoding.UTF8.GetString(requestDatas, lastSpaceCharIndex, i == requestDatas.Length ? i - lastSpaceCharIndex + 1 : i - lastSpaceCharIndex);
                            if (!string.IsNullOrEmpty(tempStr))
                            {
                                string[] formFields = tempStr.Split("&");
                                foreach (var field in formFields)
                                {
                                    string[] keyvalues = field.Split("=");
                                    if (keyvalues.Length >= 2)
                                    {
                                        if (httpRequest.Form == null)
                                        {
                                            httpRequest.Form = new Dictionary<string, string>();
                                        }
                                        httpRequest.Form[keyvalues[0]] = keyvalues[1];
                                    }
                                }
                            }
                        }
                    }
                    else
                    {
                        string tempStr = Encoding.UTF8.GetString(requestDatas, lastSpaceCharIndex, i == requestDatas.Length ? i - lastSpaceCharIndex + 1 : i - lastSpaceCharIndex);

                        if (lastSpaceCharIndex == 0)
                        {
                            string[] tempArr = tempStr.Split(" ");
                            httpRequest.Method = tempArr[0];
                            string requestUri = tempArr[1];
                            string[] urlAndQueryString = requestUri.Split("?");
                            if (urlAndQueryString.Length >= 2)
                            {
                                httpRequest.RequstUrl = urlAndQueryString[0];
                                string[] queryStrings = urlAndQueryString[1].Split("&");
                                foreach (var queryString in queryStrings)
                                {
                                    string[] keyvalues = queryString.Split("=");
                                    if (keyvalues.Length >= 2)
                                    {
                                        if (httpRequest.QueryString == null)
                                        {
                                            httpRequest.QueryString = new Dictionary<string, string>();
                                        }
                                        httpRequest.QueryString[keyvalues[0].Trim()] = keyvalues[1].Trim();
                                    }
                                }
                            }
                            else if (urlAndQueryString.Length >= 1)
                            {
                                httpRequest.RequstUrl = urlAndQueryString[0];
                            }
                            httpRequest.Version = tempArr[2];
                        }
                        else
                        {
                            //请求头
                            if (httpRequest.RequestHeaders == null)
                            {
                                httpRequest.RequestHeaders = new Dictionary<string, object>();
                            }
                            string[] tempArr = tempStr.Split(":");
                            string key = tempArr[0].Trim();
                            if (tempArr.Length >= 2)
                            {
                                string value = tempArr[1].Trim();
                                if (key.Equals("Content-Length", StringComparison.InvariantCultureIgnoreCase))
                                {
                                    int length = 0;
                                    try
                                    {
                                        length = Convert.ToInt32(value);
                                    }
                                    catch (Exception ex)
                                    {

                                    }
                                    httpRequest.RequestHeaders[key] = length;
                                }
                                else if (key.Equals("Cookie", StringComparison.InvariantCultureIgnoreCase))
                                {
                                    string[] cookies = tempArr[1].Split(";");
                                    foreach (var cookie in cookies)
                                    {
                                        string[] keyvalues = cookie.Split("=");
                                        if (keyvalues.Length >= 2)
                                        {
                                            if (httpRequest.Cookies == null)
                                            {
                                                httpRequest.Cookies = new List<HttpCookie>();
                                            }
                                            httpRequest.Cookies.Add(new HttpCookie(keyvalues[0].Trim(), keyvalues[1].Trim()));
                                        }
                                    }
                                }
                                else
                                {
                                    if (!string.IsNullOrEmpty(key))
                                    {
                                        httpRequest.RequestHeaders[key] = value;
                                    }
                                }
                            }
                            else
                            {
                                if (!string.IsNullOrEmpty(key))
                                {
                                    httpRequest.RequestHeaders[key] = "";
                                }
                            }
                        }
                    }
                    if (string.IsNullOrEmpty(fileName) || !isValue)
                    {
                        lastSpaceCharIndex = i + 2;
                    }
                    else
                    {
                        lastSpaceCharIndex = i;
                    }
                }
            }
            return httpRequest;
        }

    }
}
