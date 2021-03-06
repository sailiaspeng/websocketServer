﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Net.Sockets;
using System.Net;
using System.Text.RegularExpressions;
using System.Security.Cryptography;
using System.Diagnostics;
using Microsoft.AspNet.SignalR.Client;

namespace WebSocketServer
{
    public class Stock{
        public string Symbol;
        public string Price;
        string open;
        string high;
        string low;
        string changge;
    }
    public class WebSocket
    {
        private Dictionary<string, Session> SessionPool = new Dictionary<string, Session>();
        private Dictionary<string, string> MsgPool = new Dictionary<string, string>();
        IHubProxy indicatorHubProxy = null;
        public void send()
        {
            Console.WriteLine("do sth！");
        }
        public void ConnetHub()
        {
            var hubConnection = new HubConnection("http://128.1.10.10:12000/");
             indicatorHubProxy = hubConnection.CreateHubProxy("indicatorHub");
             //indicatorHubProxy.On<string>("setAuthorization", result=> Console.WriteLine("认证结果：" + result));
           
          //  hubConnection.Start().Wait();
            //stockTickerHubProxy.On<Stock>("UpdateStockPrice", stock => Console.WriteLine("Stock update for {0} new price {1}", stock.Symbol, stock.Price));
            //await hubConnection.Start();
            hubConnection.Start().ContinueWith(task =>
            {
                if (task.IsFaulted)
                {
                    Console.WriteLine("Failed to start: {0}", task.Exception.GetBaseException());
                }
                else
                {
                    Console.WriteLine("Success! Connected with client connection id {0}", hubConnection.ConnectionId);
                    var Object =indicatorHubProxy.Invoke<object>("setAuthorization", "authCode");
                    Console.WriteLine("message is :{0}", Object);
                    //indicatorHubProxy.On<object>("send", stock => Console.WriteLine("message is :{0}", stock));
                    //IHubProxy indicatorHubProxy = hubConnection.CreateHubProxy("indicatorHub");
                    //indicatorHubProxy.On<string>());
                    hubConnection.Received += data =>
                    {
                        Console.WriteLine(data);
                    };
                    hubConnection.Error += ex =>
                    {
                        Console.WriteLine("An error occurred {0}", ex.Message);
                    };
                    //hubConnection.Reconnected += ex =>
                    //{
                    //    Console.WriteLine("The connection was re-established");
                    //};
                    hubConnection.Closed += () =>
                    {
                        Console.WriteLine("Connection with client id {0} closed", hubConnection.ConnectionId);
                    };
                    // Do more stuff here
                }
            });
            //IHubProxy stockTickerHubProxy = hubConnection.CreateHubProxy("StockTickerHub");
            //stockTickerHubProxy.On<Stock>("UpdateStockPrice", stock => Console.WriteLine("Stock update for {0} new price {1}", stock.Symbol, stock.Price));
            //hubConnection.Start();
        }
        #region 启动WebSocket服务
        /// <summary>
        /// 启动WebSocket服务
        /// </summary>
        public void start(string serverAddress, int serverPort)
        {

            ConnetHub();

            Socket SockeServer = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            SockeServer.Bind(new IPEndPoint(IPAddress.Parse(serverAddress), serverPort));
            SockeServer.Listen(20);
            SockeServer.BeginAccept(new AsyncCallback(Accept), SockeServer);
            Trace.TraceInformation("服务已在{0}:{1}启动", serverAddress, serverPort);
            Console.WriteLine("服务已在{0}:{1}启动", serverAddress, serverPort);
            string message = "";
            while (message != "Q")
            {
                Console.WriteLine("输如消息并回车后发送");
                message = Console.ReadLine();
                SendTestMessages(message);
            }

        }
        #endregion
 
        #region 处理客户端连接请求
        /// <summary>
        /// 处理客户端连接请求
        /// </summary>
        /// <param name="result"></param>
        private void Accept(IAsyncResult socket)
        {
            // 还原传入的原始套接字
            Socket SockeServer = (Socket)socket.AsyncState;
            // 在原始套接字上调用EndAccept方法，返回新的套接字
            Socket SockeClient = SockeServer.EndAccept(socket);
            byte[] buffer = new byte[4096];
            try
            {
                //接收客户端的数据
                SockeClient.BeginReceive(buffer, 0, buffer.Length, SocketFlags.None, new AsyncCallback(Recieve), SockeClient);
                //保存登录的客户端
                Session session = new Session();
                session.SockeClient = SockeClient;
                session.IP = SockeClient.RemoteEndPoint.ToString();
                session.buffer = buffer;
                lock (SessionPool)
                {
                    if (SessionPool.ContainsKey(session.IP))
                    {
                        this.SessionPool.Remove(session.IP);
                    }
                    this.SessionPool.Add(session.IP, session);
                }
                //准备接受下一个客户端
                SockeServer.BeginAccept(new AsyncCallback(Accept), SockeServer);
                Trace.TraceInformation(string.Format("New Client {0} connected,Current Client Total:{1}", SockeClient.RemoteEndPoint, SessionPool.Count));
                Console.WriteLine(string.Format("New Client {0} connected,Current Client Total:{1}", SockeClient.RemoteEndPoint, SessionPool.Count));
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error : " + ex.ToString());
            }
        }
        public void SendTestMessages(string message)
        {
            int i = 0;
            if (SessionPool.Count == 0)
            {
                Trace.TraceInformation("No client connected!");
                Console.WriteLine("No client connected!");
                return;
            }
            else {
                byte[] msgBuffer = PackageServerData(message);
                //while (SessionPool.Count > 0)
                //{
                    foreach (Session se in SessionPool.Values)
                    {
                        se.SockeClient.Send(msgBuffer, msgBuffer.Length, SocketFlags.None);
                        Trace.TraceInformation("Sent Message:{0} To {1} ,totalClient:{2} ,index:{3}", message, se.IP, SessionPool.Count, i++);
                        Console.WriteLine("Sent Message:{0} To {1} ,totalClient:{2} ,index:{3}", message, se.IP, SessionPool.Count, i++);

                    }
                //    Thread.Sleep(1000);
                //}
            }
                
        }
        #endregion
 
        #region 处理接收的数据
        /// <summary>
        /// 处理接受的数据
        /// </summary>
        /// <param name="socket"></param>
        private void Recieve(IAsyncResult socket)
        {
            Socket SockeClient = (Socket)socket.AsyncState;
           
            string IP = SockeClient.RemoteEndPoint.ToString();
            if (SockeClient == null || !SessionPool.ContainsKey(IP))
            {
                return;
            }
            try
            {
                int length = SockeClient.EndReceive(socket);
                byte[] buffer = SessionPool[IP].buffer;
                SockeClient.BeginReceive(buffer, 0, buffer.Length, SocketFlags.None, new AsyncCallback(Recieve), SockeClient);
                string msg = Encoding.UTF8.GetString(buffer, 0, length);
                //  websocket建立连接的时候，除了TCP连接的三次握手，websocket协议中客户端与服务器想建立连接需要一次额外的握手动作
                if (msg.Contains("Sec-WebSocket-Key"))
                {
                    SockeClient.Send(PackageHandShakeData(buffer, length));
                    SessionPool[IP].isWeb = true;
                   // SendTestMessages();
                    return;
                }
                if (SessionPool[IP].isWeb)
                {
                    msg = AnalyzeClientData(buffer, length);
                    if(msg=="close"){
                        //SockeClient.Send(PackageHandShakeData(buffer, length))
                        SockeClient.DisconnectAsync( new SocketAsyncEventArgs());
                       // SockeClient.Disconnect(true);
                        Console.WriteLine("客户端 {0} 断开连接", IP);
                       // SockeClient.EndReceive(socket);
                        SockeClient.Shutdown(SocketShutdown.Both);
                        SessionPool[IP].SockeClient.Close();

                        SessionPool.Remove(IP);
                        return;
                    }
                    Console.WriteLine("Received Message:{0} From {1}", msg, SessionPool[IP].IP);
                }
                byte[] msgBuffer = PackageServerData(msg);

                try
                {
                    foreach (Session se in SessionPool.Values)
                    {
                        if (se.IP != IP)
                        {

                            se.SockeClient.Send(msgBuffer, msgBuffer.Length, SocketFlags.None);
                            Console.WriteLine("Sent Message:{0} To {1}", msg, se.IP);
                        }
                        else {
                            string bac = "消息已发送！";
                            byte[] backMsg = PackageServerData(bac);
                            se.SockeClient.Send(backMsg, backMsg.Length, SocketFlags.None);
                        }

                    }
                }
                catch (Exception ex)
                {
                    
                    Console.WriteLine(ex.ToString());
                }
                
            }
            catch (Exception ex)
            {
                //Console.Write("shutdowsn");
                //SockeClient.Shutdown( SocketShutdown.Both);
                SockeClient.Disconnect(true);
                Console.WriteLine("客户端 {0} 断开连接", IP);
                SessionPool.Remove(IP);

            }
        }
        #endregion
 
        #region 客户端和服务端的响应
        /*
         * 客户端向服务器发送请求
         * 
         * GET / HTTP/1.1
         * Origin: http://localhost:1416
         * Sec-WebSocket-Key: vDyPp55hT1PphRU5OAe2Wg==
         * Connection: Upgrade
         * Upgrade: Websocket
         *Sec-WebSocket-Version: 13
         * User-Agent: Mozilla/5.0 (Windows NT 6.1; WOW64; Trident/7.0; rv:11.0) like Gecko
         * Host: localhost:8064
         * DNT: 1
         * Cache-Control: no-cache
         * Cookie: DTRememberName=admin
         * 
         * 服务器给出响应
         * 
         * HTTP/1.1 101 Switching Protocols
         * Upgrade: websocket
         * Connection: Upgrade
         * Sec-WebSocket-Accept: xsOSgr30aKL2GNZKNHKmeT1qYjA=
         * 
         * 在请求中的“Sec-WebSocket-Key”是随机的，服务器端会用这些数据来构造出一个SHA-1的信息摘要。把“Sec-WebSocket-Key”加上一个魔幻字符串
         * “258EAFA5-E914-47DA-95CA-C5AB0DC85B11”。使用 SHA-1 加密，之后进行 BASE-64编码，将结果做为 “Sec-WebSocket-Accept” 头的值，返回给客户端
         */
        #endregion
 
        #region 打包请求连接数据
        /// <summary>
        /// 打包请求连接数据
        /// </summary>
        /// <param name="handShakeBytes"></param>
        /// <param name="length"></param>
        /// <returns></returns>
        private byte[] PackageHandShakeData(byte[] handShakeBytes, int length)
        {
            string handShakeText = Encoding.UTF8.GetString(handShakeBytes, 0, length);
            string key = string.Empty;
            Regex reg = new Regex(@"Sec\-WebSocket\-Key:(.*?)\r\n");
            Match m = reg.Match(handShakeText);
            if (m.Value != "")
            {
                key = Regex.Replace(m.Value, @"Sec\-WebSocket\-Key:(.*?)\r\n", "$1").Trim();
            }
            byte[] secKeyBytes = SHA1.Create().ComputeHash(Encoding.ASCII.GetBytes(key + "258EAFA5-E914-47DA-95CA-C5AB0DC85B11"));
            string secKey = Convert.ToBase64String(secKeyBytes);
            var responseBuilder = new StringBuilder();
            responseBuilder.Append("HTTP/1.1 101 Switching Protocols" + "\r\n");
            responseBuilder.Append("Upgrade: websocket" + "\r\n");
            responseBuilder.Append("Connection: Upgrade" + "\r\n");
            responseBuilder.Append("Sec-WebSocket-Accept: " + secKey + "\r\n\r\n");
            return Encoding.UTF8.GetBytes(responseBuilder.ToString());
        }
        #endregion
 
        #region 处理接收的数据
        /// <summary>
        /// 处理接收的数据
        /// 参考 http://www.cnblogs.com/smark/archive/2012/11/26/2789812.html
        /// </summary>
        /// <param name="recBytes"></param>
        /// <param name="length"></param>
        /// <returns></returns>
        private string AnalyzeClientData(byte[] recBytes, int length)
        {
            int start = 0;
            // 如果有数据则至少包括3位
            if (length < 2) return "";
            // 判断是否为结束针
            bool IsEof = (recBytes[start] >> 7) > 0;
            // 暂不处理超过一帧的数据
            if (!IsEof) return "";
            start++;
            // 是否包含掩码
            bool hasMask = (recBytes[start] >> 7) > 0;
            // 不包含掩码的暂不处理
            if (!hasMask) return "";
            // 获取数据长度
            UInt64 mPackageLength = (UInt64)recBytes[start] & 0x7F;
            start++;
            // 存储4位掩码值
            byte[] Masking_key = new byte[4];
            // 存储数据
            byte[] mDataPackage;
            if (mPackageLength == 126)
            {
                // 等于126 随后的两个字节16位表示数据长度
                mPackageLength = (UInt64)(recBytes[start] << 8 | recBytes[start + 1]);
                start += 2;
            }
            if (mPackageLength == 127)
            {
                // 等于127 随后的八个字节64位表示数据长度
                mPackageLength = (UInt64)(recBytes[start] << (8 * 7) | recBytes[start] << (8 * 6) | recBytes[start] << (8 * 5) | recBytes[start] << (8 * 4) | recBytes[start] << (8 * 3) | recBytes[start] << (8 * 2) | recBytes[start] << 8 | recBytes[start + 1]);
                start += 8;
            }
            mDataPackage = new byte[mPackageLength];
            for (UInt64 i = 0; i < mPackageLength; i++)
            {
                mDataPackage[i] = recBytes[i + (UInt64)start + 4];
            }
            Buffer.BlockCopy(recBytes, start, Masking_key, 0, 4);
            for (UInt64 i = 0; i < mPackageLength; i++)
            {
                mDataPackage[i] = (byte)(mDataPackage[i] ^ Masking_key[i % 4]);
            }
            return Encoding.UTF8.GetString(mDataPackage);
        }
        #endregion
 
        #region 发送数据
        /// <summary>
        /// 把发送给客户端消息打包处理（拼接上谁什么时候发的什么消息）
        /// </summary>
        /// <returns>The data.</returns>
        /// <param name="message">Message.</param>
        private byte[] PackageServerData(string msg)
        {
            byte[] content = null;
            byte[] temp = Encoding.UTF8.GetBytes(msg);
            if (temp.Length < 126)
            {
                content = new byte[temp.Length + 2];
                content[0] = 0x81;
                content[1] = (byte)temp.Length;
                Buffer.BlockCopy(temp, 0, content, 2, temp.Length);
            }
            else if (temp.Length < 0xFFFF)
            {
                content = new byte[temp.Length + 4];
                content[0] = 0x81;
                content[1] = 126;
                content[2] = (byte)(temp.Length & 0xFF);
                content[3] = (byte)(temp.Length >> 8 & 0xFF);
                Buffer.BlockCopy(temp, 0, content, 4, temp.Length);
            }
            return content;
        }
        #endregion
    }
}