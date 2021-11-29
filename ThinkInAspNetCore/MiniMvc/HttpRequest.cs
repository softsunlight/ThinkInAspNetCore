using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;

namespace ThinkInAspNetCore.MiniMvc
{
    /// <summary>
    /// http请求类
    /// </summary>
    public class HttpRequest
    {
        /// <summary>
        /// 获取url或表单域中的参数
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public string this[string key]
        {
            get
            {
                return QueryString.ContainsKey(key) ? QueryString[key] : Form[key];
            }
        }
        /// <summary>
        /// 请求方法
        /// </summary>
        public string Method { get; set; }
        /// <summary>
        /// 请求地址
        /// </summary>
        public string RequstUrl { get; set; }
        /// <summary>
        /// http版本
        /// </summary>
        public string Version { get; set; }
        /// <summary>
        /// http请求头
        /// </summary>
        public Dictionary<string, object> RequestHeaders { get; set; }
        /// <summary>
        /// Host
        /// </summary>
        public string Host { get; set; }
        /// <summary>
        /// Connection
        /// </summary>
        public string Connection { get; set; }
        /// <summary>
        /// Content-Type
        /// </summary>
        public string ContentType { get; set; }
        /// <summary>
        /// Content-Length
        /// </summary>
        public int ContentLength { get; set; }
        /// <summary>
        /// Cache-Control
        /// </summary>
        public string CacheControl { get; set; }
        /// <summary>
        /// User-Agent
        /// </summary>
        public string UserAgent { get; set; }
        /// <summary>
        /// Accept
        /// </summary>
        public string Accept { get; set; }
        /// <summary>
        /// Referer
        /// </summary>
        public string Referer { get; set; }
        /// <summary>
        /// url地址中的请求参数
        /// </summary>
        public Dictionary<string, string> QueryString { get; set; }
        /// <summary>
        /// 表单数据
        /// </summary>
        public Dictionary<string, string> Form { get; set; }
        /// <summary>
        /// Http请求文件集合
        /// </summary>
        public List<HttpFile> Files { get; set; }
        /// <summary>
        /// Cookie集合
        /// </summary>
        public List<HttpCookie> Cookies { get; set; }
        /// <summary>
        /// 请求流
        /// </summary>
        public NetworkStream RequestStream { get; set; }
    }
}
