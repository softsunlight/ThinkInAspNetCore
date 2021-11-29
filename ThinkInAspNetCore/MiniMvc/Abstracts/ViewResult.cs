using System;
using System.Collections.Generic;
using System.Text;
using ThinkInAspNetCore.MiniMvc.Controllers;

namespace ThinkInAspNetCore.MiniMvc.Abstracts
{
    public class ViewResult : IActionResult
    {
        private Controller controller;

        public ViewResult()
        {
            
        }

        public ViewResult(Controller controller)
        {
            this.controller = controller;
        }

        public void Render()
        {

        }
    }
}
