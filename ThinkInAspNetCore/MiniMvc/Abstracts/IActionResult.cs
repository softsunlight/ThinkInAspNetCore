using System;
using System.Collections.Generic;
using System.Text;

namespace ThinkInAspNetCore.MiniMvc.Abstracts
{
    /// <summary>
    /// http response
    /// </summary>
    public interface IActionResult
    {
        /// <summary>
        /// 渲染http响应
        /// </summary>
        void Render();
    }
}
