using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;

namespace ThinkInAspNetCore.MiniMvc
{
    /// <summary>
    /// http响应类
    /// </summary>
    public class HttpResponse
    {
        /// <summary>
        /// http版本
        /// </summary>
        public string Version { get; set; }
        /// <summary>
        /// 响应码
        /// </summary>
        public string StatusCode { get; set; }
        /// <summary>
        /// 相应消息
        /// </summary>
        public string StatusMessage { get; set; }
        /// <summary>
        /// http响应头
        /// </summary>
        public Dictionary<string, object> ResponseHeaders { get; set; }
        /// <summary>
        /// Content-Type
        /// </summary>
        public string ContentType
        {
            get
            {
                return ResponseHeaders == null || !ResponseHeaders.ContainsKey("Content-Type") ? "" : ResponseHeaders["Content-Type"].ToString();
            }
            set
            {
                if (ResponseHeaders == null)
                {
                    ResponseHeaders = new Dictionary<string, object>();
                }
                ResponseHeaders["Content-Type"] = value;
            }
        }
        /// <summary>
        /// Content-Length
        /// </summary>
        public int ContentLength
        {
            get
            {
                int length = 0;
                try
                {
                    if (ResponseHeaders != null && ResponseHeaders.ContainsKey("Content-Length"))
                    {
                        length = Convert.ToInt32(ResponseHeaders["Content-Length"]);
                    }
                }
                catch (Exception ex)
                {

                }
                return length;
            }
            set
            {
                if (ResponseHeaders == null)
                {
                    ResponseHeaders = new Dictionary<string, object>();
                }
                ResponseHeaders["Content-Length"] = value;
            }
        }
        ///// <summary>
        ///// Server
        ///// </summary>
        //public string Server { get; set; }
        ///// <summary>
        ///// Date
        ///// </summary>
        //public DateTime Date { get; set; }
        /// <summary>
        /// 响应体
        /// </summary>
        public string ResponseBody { get; set; }
        /// <summary>
        /// 响应流
        /// </summary>
        public NetworkStream ResponseStream { get; set; }

        /// <summary>
        /// 响应回写Cookie
        /// </summary>
        public List<HttpCookie> Cookies { get; set; }

        /// <summary>
        /// 写入网络流中
        /// </summary>
        public void Write()
        {
            StringBuilder stringBuilder = new StringBuilder();
            //构造相应头
            stringBuilder.Append(Version).Append(" ").Append(StatusCode).Append(" ").Append(StatusMessage).Append(Environment.NewLine);
            if (ResponseHeaders != null && ResponseHeaders.Count > 0)
            {
                foreach (string key in ResponseHeaders.Keys)
                {
                    stringBuilder.Append(key).Append(":").Append(ResponseHeaders[key]).Append(Environment.NewLine);
                }
            }
            if (Cookies != null && Cookies.Count > 0)
            {
                foreach (var cookie in Cookies)
                {
                    stringBuilder.Append("Set-Cookie").Append(":").Append(cookie.Name).Append("=").Append(cookie.Value).Append(";");
                    if (!string.IsNullOrEmpty(cookie.Path))
                    {
                        stringBuilder.Append("Path=").Append(cookie.Path).Append(";");
                    }
                    if (cookie.Secure)
                    {
                        stringBuilder.Append("Secure;");
                    }
                    if (cookie.HttpOnly)
                    {
                        stringBuilder.Append("HttpOnly;");
                    }
                    stringBuilder.Append(Environment.NewLine);
                }
            }
            if (!string.IsNullOrEmpty(ResponseBody))
            {
                stringBuilder.Append(Environment.NewLine);
                stringBuilder.Append(ResponseBody);
            }
            if (ResponseStream != null)
            {
                ResponseStream.Write(Encoding.UTF8.GetBytes(stringBuilder.ToString()));
            }
        }

    }
}
