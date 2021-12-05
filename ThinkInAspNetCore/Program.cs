using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using ThinkInAspNetCore.MiniMvc;

namespace ThinkInAspNetCore
{
    class Program
    {
        static void Main(string[] args)
        {
            new WebApplication(args).Run();
        }

    }
}
