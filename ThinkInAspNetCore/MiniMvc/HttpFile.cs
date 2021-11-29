using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace ThinkInAspNetCore.MiniMvc
{
    /// <summary>
    /// http请求文件类
    /// </summary>
    public class HttpFile
    {
        /// <summary>
        /// 表单域name名称
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// 上传的文件名称
        /// </summary>
        public string FileName { get; set; }
        /// <summary>
        /// 文件字节数据
        /// </summary>
        public byte[] FileDatas { get; set; }
        //文件类型
        public string ContentType { get; set; }
        /// <summary>
        /// 写入服务器磁盘
        /// </summary>
        /// <param name="filePath"></param>
        public void Write(string filePath)
        {
            try
            {
                if (!string.IsNullOrEmpty(filePath) && FileDatas.Length > 0)
                {
                    File.WriteAllBytes(filePath, FileDatas);
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

    }
}
