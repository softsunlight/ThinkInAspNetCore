using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace ThinkInAspNetCore.MiniMvc.WebSocket
{
    /// <summary>
    /// WebSocket工具类
    /// </summary>
    public class WebSocketUtils
    {
        /// <summary>
        /// 构造从客户端发送给服务端的数据帧
        /// </summary>
        /// <param name="datas"></param>
        /// <returns></returns>
        public WebSocketFrame GetWebSocketFrame(byte[] datas)
        {
            if (datas == null || datas.Length <= 0)
            {
                return null;
            }
            WebSocketFrame webSocketFrame = new WebSocketFrame();
            //先读第一个字节(FIN(1) RSV(1) RSV(1) RSV(1) opcode(4))
            BitArray firstByteBitArray = new BitArray(new byte[1] { datas[0] });
            webSocketFrame.Fin = firstByteBitArray.Get(firstByteBitArray.Length - 1);
            webSocketFrame.Rsv1 = firstByteBitArray.Get(firstByteBitArray.Length - 2);
            webSocketFrame.Rsv2 = firstByteBitArray.Get(firstByteBitArray.Length - 3);
            webSocketFrame.Rsv3 = firstByteBitArray.Get(firstByteBitArray.Length - 4);
            BitArray opcodeBitArray = new BitArray(4);
            for (var i = 0; i < 4; i++)
            {
                opcodeBitArray.Set(i, firstByteBitArray[i]);
            }
            int[] opcodeArray = new int[1];
            opcodeBitArray.CopyTo(opcodeArray, 0);
            webSocketFrame.Opcode = opcodeArray[0];
            //第二个字节(mask(1) 'payload len'(7))
            BitArray secondByteBitArray = new BitArray(new byte[1] { datas[1] });
            BitArray payloadLenBitArray = new BitArray(7);
            for (var i = 0; i < 7; i++)
            {
                payloadLenBitArray.Set(i, secondByteBitArray[i]);
            }
            int[] payloadLenArray = new int[1];
            payloadLenBitArray.CopyTo(payloadLenArray, 0);
            webSocketFrame.PayloadLen = payloadLenArray[0];
            long realLen = webSocketFrame.PayloadLen;
            int maskKeyStart = 2;
            if (webSocketFrame.PayloadLen == 126)
            {
                realLen = BitConverter.ToUInt16(datas, 2);
                maskKeyStart = 4;
            }
            else if (webSocketFrame.PayloadLen == 127)
            {
                realLen = BitConverter.ToInt64(datas, 2);
                maskKeyStart = 12;
            }
            webSocketFrame.ExtPayloadLen = realLen;
            //Mask
            byte[] maskKeyBytes = new byte[4];
            if (secondByteBitArray.Get(secondByteBitArray.Length - 1))
            {
                Array.Copy(datas, maskKeyStart, maskKeyBytes, 0, maskKeyBytes.Length);
            }
            webSocketFrame.MaskingKey = maskKeyBytes;
            if (realLen > 0)
            {
                //数据
                byte[] encodeDatas = new byte[realLen];
                Array.Copy(datas, maskKeyStart + 4, encodeDatas, 0, realLen);
                //解码
                byte[] decodeDatas = new byte[realLen];
                for (var i = 0; i < encodeDatas.Length; i++)
                {
                    decodeDatas[i] = (byte)(encodeDatas[i] ^ maskKeyBytes[i % 4]);
                }
                webSocketFrame.PayloadData = decodeDatas;
            }
            return webSocketFrame;
        }
    }
}
