using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;

namespace SoftSunlight.MiniMvc.WebSocket
{
    public class WebSocketManager
    {
        private static Dictionary<TcpClient, WebSocket> client2WebSocket = new Dictionary<TcpClient, WebSocket>();

        public static void Add(TcpClient tcpClient)
        {
            if (!client2WebSocket.ContainsKey(tcpClient))
            {
                client2WebSocket[tcpClient] = new WebSocket(tcpClient);
            }
        }

        public static WebSocket Get(TcpClient tcpClient)
        {
            if (client2WebSocket.ContainsKey(tcpClient))
            {
                return client2WebSocket[tcpClient];
            }
            return null;
        }

    }
}
