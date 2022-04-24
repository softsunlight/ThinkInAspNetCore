using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using SoftSunlight.MiniMvc;
using SoftSunlight.MiniMvc.Extensions;

namespace ThinkInAspNetCore
{
    class Program
    {
        static void Main(string[] args)
        {
            WebApplication webApplication = new WebApplication(args);
            webApplication.UseStaticFile();
            webApplication.UseCors();
            webApplication.UseWebSocket();
            webApplication.UseMvc();
            webApplication.Run();
        }

    }
}
