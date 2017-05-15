using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;
using System.Threading;
using System.Net;
using System.Configuration;
using WebsocketServer;
using System.Diagnostics;
 
namespace WebSocketServer
{
    class Program
    {
        static void Main(string[] args)
        {
            WebSocket socket = new WebSocket();
            LogListener listener = new LogListener();
            Trace.Listeners.Add(listener);
            try
            {
                string ip = ConfigurationManager.AppSettings["serverAddress"];
                int port = int.Parse(ConfigurationManager.AppSettings["serverPort"]);
                socket.start(ip, port);
            }
            catch (Exception ex) {
                Trace.TraceInformation("消息服务器启动失败！" + ex.Message);
                Thread.Sleep(5);
            }
        }
    }
}
