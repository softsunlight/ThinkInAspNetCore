using System;
using System.Collections;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;

namespace ThinkInAspNetCore.MiniMvc.WebSocket
{
    public class WebSocket
    {
        private TcpClient client;
        public WebSocket(TcpClient tcpClient)
        {
            this.client = tcpClient;
        }

        /// <summary>
        /// 消息处理事件
        /// </summary>
        public Action<byte[]> OnMessage { get; set; }

        /// <summary>
        /// 发送文本消息
        /// </summary>
        /// <param name="txtMessage"></param>
        public void Send(string txtMessage)
        {
            byte[] payloadDatas = Encoding.UTF8.GetBytes(txtMessage);
            Send(OpcodeEnum.Text, payloadDatas);
        }

        /// <summary>
        /// 发送二进制数据
        /// </summary>
        /// <param name="datas"></param>
        public void Send(byte[] datas)
        {
            Send(OpcodeEnum.Binary, datas);
        }

        /// <summary>
        /// 发送数据
        /// </summary>
        /// <param name="opcode"></param>
        /// <param name="sendDatas"></param>
        private void Send(OpcodeEnum opcode, byte[] sendDatas)
        {
            List<byte> frameByteList = new List<byte>();
            frameByteList.Add((byte)(128 + opcode));
            if (sendDatas.Length <= 125)
            {
                frameByteList.Add((byte)(sendDatas.Length));
            }
            else if (sendDatas.Length <= ushort.MaxValue)
            {
                frameByteList.Add((byte)126);
                //高位在前
                List<byte> payloadLenBytes = new List<byte>(BitConverter.GetBytes((ushort)sendDatas.Length));
                payloadLenBytes.Reverse();
                frameByteList.AddRange(payloadLenBytes.ToArray());
            }
            else
            {
                frameByteList.Add(127);
                List<byte> payloadLenBytes = new List<byte>(BitConverter.GetBytes((long)sendDatas.Length));
                payloadLenBytes.Reverse();
                frameByteList.AddRange(payloadLenBytes);
            }
            frameByteList.AddRange(sendDatas);
            if (client.Connected)
            {
                NetworkStream networkStream = null;
                try
                {
                    networkStream = client.GetStream();
                    networkStream.Write(frameByteList.ToArray());
                }
                catch (Exception ex)
                {

                }
            }
        }
    }
}
