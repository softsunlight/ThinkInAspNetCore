using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
                        List<byte> requestByteList = new List<byte>();
                        while (true)
                        {
                            //http1.1 默认是持久链接
                            try
                            {
                                NetworkStream networkStream = tcpClient.GetStream();
                                MemoryStream memoryStream = new MemoryStream();
                                int recvTotals = 0;
                                int realLength = 0;
                                //是否完整的读取了请求信息
                                bool isReadEnd = false;
                                do
                                {
                                    byte[] buffer = new byte[512];
                                    realLength = networkStream.Read(buffer, 0, buffer.Length);
                                    memoryStream.Write(buffer, 0, realLength);
                                    recvTotals += realLength;
                                } while (networkStream.DataAvailable);
                                if (recvTotals > 0)
                                {
                                    HttpHandler httpHandler = null;
                                    HttpRequest httpRequest = new HttpRequest();
                                    try
                                    {
                                        requestByteList.AddRange(memoryStream.ToArray());
                                        if (memoryStream != null)
                                        {
                                            memoryStream.Close();
                                            memoryStream.Dispose();
                                            memoryStream = null;
                                        }
                                        if (IsWebSocket(requestByteList.ToArray()))
                                        {
                                            isReadEnd = true;
                                            httpRequest = BuildWebSocketRequest(tcpClient, requestByteList.ToArray());
                                        }
                                        else
                                        {
                                            int requestBodyStart = 0;
                                            int lastSpaceCharIndex = 0;
                                            httpRequest = GetRequestHeader(httpRequest, requestByteList.ToArray(), out requestBodyStart, out lastSpaceCharIndex);
                                            if (httpRequest == null)
                                            {
                                                throw new Exception("请求内容为空");
                                            }
                                            if (httpRequest.RequestHeaders.ContainsKey("Content-Length"))
                                            {
                                                int contentLength = Convert.ToInt32(httpRequest.RequestHeaders["Content-Length"]);
                                                if (requestByteList.Count >= contentLength)
                                                {
                                                    isReadEnd = true;
                                                    string content = Encoding.UTF8.GetString(requestByteList.ToArray(), 0, requestBodyStart);
                                                    Log.Write(content);
                                                    httpRequest = BuildRequest(tcpClient, requestByteList.ToArray());
                                                }
                                            }
                                            else
                                            {
                                                isReadEnd = true;
                                                string content = Encoding.UTF8.GetString(requestByteList.ToArray(), 0, requestBodyStart);
                                                Log.Write(content);
                                                httpRequest = BuildRequest(tcpClient, requestByteList.ToArray());
                                            }
                                            httpHandler = new HttpHandler(httpRequest);
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
                                        if (isReadEnd)
                                        {
                                            if (httpHandler != null)
                                            {
                                                httpHandler.ProcessRequest();
                                            }
                                            if (requestByteList != null && requestByteList.Count > 0)
                                            {
                                                requestByteList.Clear();
                                            }
                                        }
                                    }
                                }
                                else
                                {
                                    //不知道为什么，每次有个空连接
                                    if (tcpClient != null)
                                    {
                                        tcpClient.Close();
                                        tcpClient.Dispose();
                                        tcpClient = null;
                                    }
                                    break;
                                }
                            }
                            catch (Exception ex)
                            {
                                if (tcpClient != null)
                                {
                                    tcpClient.Close();
                                    tcpClient.Dispose();
                                    tcpClient = null;
                                }
                                break;
                            }
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
        /// 获取请求头
        /// </summary>
        /// <param name="requestDatas"></param>
        /// <param name="requestBodyStart"></param>
        /// <returns></returns>
        private static HttpRequest GetRequestHeader(HttpRequest httpRequest, byte[] requestDatas, out int requestBodyStart, out int lastSpaceCharIndex)
        {
            if (requestDatas.Length <= 0)
            {
                throw new Exception("请求内容为空");
            }
            requestBodyStart = int.MaxValue;
            lastSpaceCharIndex = 0;
            for (int i = 0; i < requestDatas.Length; i++)
            {
                if ((requestDatas[i] == 13 && requestDatas[i + 1] == 10) || i == requestDatas.Length - 1)
                {
                    //遇到空行，则下一行是表单域
                    if (i - lastSpaceCharIndex == 0)
                    {
                        if (requestBodyStart == int.MaxValue)
                        {
                            requestBodyStart = i + 2;
                            lastSpaceCharIndex = requestBodyStart;
                        }
                        break;
                    }
                    if (i <= lastSpaceCharIndex)
                    {
                        continue;
                    }
                    if (i < requestBodyStart)
                    {
                        string tempStr = Encoding.UTF8.GetString(requestDatas.ToArray(), lastSpaceCharIndex, i == requestDatas.Length ? i - lastSpaceCharIndex + 1 : i - lastSpaceCharIndex);

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
                                            httpRequest.QueryString = new Dictionary<string, List<string>>();
                                        }
                                        httpRequest.QueryString[keyvalues[0].Trim()].Add(keyvalues[1].Trim());
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
                            Match m = Regex.Match(tempStr, @"(?is)(?<key>[^:]*)\s*:\s*(?<value>.*)");
                            if (m.Success)
                            {
                                string key = m.Groups["key"].Value;
                                string value = m.Groups["value"].Value;
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
                                    string[] cookies = value.Split(";");
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
                        }
                    }
                    {
                        lastSpaceCharIndex = i + 2;
                    }
                }
            }
            return httpRequest;
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
                                i += 2;

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
                                isValue = false;
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
                                            httpRequest.Form = new Dictionary<string, List<string>>();
                                        }
                                        if (!httpRequest.Form.ContainsKey(name))
                                        {
                                            httpRequest.Form[name] = new List<string>();
                                        }
                                        httpRequest.Form[name].Add(tempStr);
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
                                            httpRequest.Form = new Dictionary<string, List<string>>();
                                        }
                                        if (!httpRequest.Form.ContainsKey(name))
                                        {
                                            httpRequest.Form[name] = new List<string>();
                                        }
                                        httpRequest.Form[keyvalues[0]].Add(keyvalues[1]);
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
                                            httpRequest.QueryString = new Dictionary<string, List<string>>();
                                        }
                                        if (!httpRequest.QueryString.ContainsKey(keyvalues[0].Trim()))
                                        {
                                            httpRequest.QueryString[keyvalues[0].Trim()] = new List<string>();
                                        }
                                        httpRequest.QueryString[keyvalues[0].Trim()].Add(keyvalues[1].Trim());
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
                            Match m = Regex.Match(tempStr, @"(?is)(?<key>[^:]*)\s*:\s*(?<value>.*)");
                            if (m.Success)
                            {
                                string key = m.Groups["key"].Value;
                                string value = m.Groups["value"].Value;
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
                                    string[] cookies = value.Split(";");
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

        /// <summary>
        /// 判断是否是websocket数据
        /// </summary>
        /// <param name="requestDatas"></param>
        /// <returns></returns>
        private bool IsWebSocket(byte[] requestDatas)
        {
            //刺探性的获取前10个字节
            int length = 10;
            if (length > requestDatas.Length)
            {
                length = requestDatas.Length;
            }
            byte[] bytes = new byte[length];
            Array.Copy(requestDatas, bytes, length);
            string str = Encoding.UTF8.GetString(bytes);
            Type type = typeof(HttpRequestMethod);
            if (!Regex.IsMatch(str, @"(?is)" + string.Join("|", type.GetFields().Select(p => p.Name))))
            {
                return true;
            }
            return false;
        }

        /// <summary>
        /// 构造WebSocket请求类
        /// </summary>
        /// <param name="tcpClient"></param>
        /// <param name="requestDatas"></param>
        /// <returns></returns>
        private static HttpRequest BuildWebSocketRequest(TcpClient tcpClient, byte[] requestDatas)
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
            //先读第一个字节(FIN(1) RSV(1) RSV(1) RSV(1) opcode(4))
            BitArray firstByteArray = new BitArray(new byte[1] { requestDatas[0] });
            if (firstByteArray.Get(0))//消息最后一个frame
            {

            }
            int[] opcodeArray = new int[1];
            for (var i = 4; i < firstByteArray.Length; i++)
            {
                firstByteArray.CopyTo(opcodeArray, 0);
            }
            int opcode = opcodeArray[0];
            //第二个字节(mask(1) 'payload len'(7))
            BitArray secondByteArray = new BitArray(new byte[1] { requestDatas[1] });
            ushort[] payloadLenArray = new ushort[1];
            for (var i = 1; i < secondByteArray.Length; i++)
            {
                secondByteArray.CopyTo(payloadLenArray, 0);
            }
            int payloadLen = payloadLenArray[0];
            long realLen = payloadLen;
            if (payloadLen == 126)
            {
                realLen = BitConverter.ToUInt16(requestDatas, 2);
            }
            else if (payloadLen == 127)
            {
                realLen = BitConverter.ToInt64(requestDatas, 2);
            }
            //Mask
            int maskKey = 0;
            if (secondByteArray.Get(0))
            {
                maskKey = BitConverter.ToInt32(requestDatas, 10);
            }
            //数据
            byte[] datas = new byte[realLen];
            Array.Copy(requestDatas, 14, datas, 0, realLen);
            return httpRequest;
        }

    }
}
