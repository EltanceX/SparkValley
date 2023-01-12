using Newtonsoft.Json;
using WebSocketSharp.Server;
using WebSocketSharp;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System;
using System.Linq;

namespace main
{
    class main
    {
        public static void Main()
        {
            Console.WriteLine("SparkValley v0.0 is running !");
            var ws = new WSocket(3000);
            ws.Start();
            while (true)
            {
                Console.ReadLine();
            }
        }
    }
    public class gameUtil
    {
        public WSocket ws;
        public delegate void cmdCallback(JObject json);
        public static Dictionary<string, cmdCallback> cmdQueue;
        public static string uuidv4() { return System.Guid.NewGuid().ToString(); }

        public gameUtil(WSocket ws)
        {
            this.ws = ws;
            gameUtil.cmdQueue = new Dictionary<string, cmdCallback>();
        }
    }
    //Server
    public class WSocket
    {
        //WebSocket对象
        public WebSocketServer _socketServer = null;
        List<WebSocketServiceHost> webSocketServiceHosts;
        //public delegate bool SendDatas(string sData);
        //public static event SendDatas SendDataEvent;
        public WSocket(int port)
        {
            //实例化
            _socketServer = new WebSocketServer("ws://0.0.0.0:" + port);
            Console.WriteLine($"SparkValley Websocket Server listening on port {port}");
            //_socketServer.WaitTime = new TimeSpan(100);
            //添加WebSocket的服务处理类
            _socketServer.AddWebSocketService<WSocketHandle>("/");
            var wSocketServiceManager = _socketServer.WebSocketServices;
            webSocketServiceHosts = wSocketServiceManager.Hosts.ToList();
            //Console.WriteLine(webSocketServiceHosts[0].Sessions);


        }
        public async void sendall(string data)
        {
            var session = webSocketServiceHosts[0].Sessions;
            foreach (var sid in session.IDs)
            {
                Console.WriteLine($"Sending data <String>\"{data}\" to <WS.Client.ID>\"{sid}\"");
                session.SendToAsync(data, sid, new Action<bool>(t =>
                {
                    if (!t)
                    {
                        Console.WriteLine("发送失败");
                    }
                }));
            }
        }
        //public void SendData(string Data)
        //{
        //    //第一种触发方式发送
        //    // SendDataEvent?.Invoke(Data);
        //    //第二种获取到session会话对象发送
        //    var session = webSocketServiceHosts[0].Sessions;//获取到session会话对象
        //    var sessionIds = session.ActiveIDs;//获取会话的IDs
        //    foreach (var sessionId in sessionIds)
        //    {
        //        //发送
        //        session.SendToAsync(Data, sessionId, new Action<bool>(t =>
        //        {
        //            if (!t)
        //            {
        //                Console.WriteLine("发送失败");
        //            }
        //        }));
        //    }
        //}
        public void Start()
        {
            _socketServer.Start();
        }
        public void Stop()
        {
            _socketServer.Stop();
        }
    }
    /// Websoket的信息处理类
    public class WSocketHandle : WebSocketBehavior
    {
        public WSocketHandle()
        {
            Console.WriteLine("constractor");
        }
        //public WSocketHandle()
        //{
        //    WSocket.SendDataEvent += SendDataEvent;
        //}
        protected override void OnClose(CloseEventArgs e)
        {
            Console.WriteLine("A connection Closed. Current number of connections: " + Sessions.Count);
        }
        protected override void OnOpen()
        {
            Console.WriteLine("A connection Established. Current number of connections: " + Sessions.Count);
            this.Send(MinecraftJsonFormat.subscribe());
        }
        protected override void OnError(WebSocketSharp.ErrorEventArgs e)
        {
            Console.WriteLine("An Error occurred ! [at Websocket.OnError]");
            Console.WriteLine("Error Data: " + e.Message);
        }
        protected override void OnMessage(MessageEventArgs e)
        {
            string data = e.Data;
            JObject json = (JObject)JsonConvert.DeserializeObject(data);
            Console.WriteLine(json);
        }
    }
    class MinecraftJsonFormat
    {
        public static string subscribe(string ev = "PlayerMessage")
        {
            return @"{
    ""body"": {
        ""eventName"": """ + ev + @"""
    },
    ""header"": {
        ""requestId"": ""00000000-0000-0000-0000-000000000005"",
        ""messagePurpose"": ""subscribe"",
        ""version"": 1,
        ""messageType"": ""commandRequest""
    }
}";
        }
    }
}

//var server = new WebSocketServer("ws://127.0.0.1:3000");
//FleckLog.LogAction = (level, message, ex) =>
//{
//    switch (level)
//    {
//        case LogLevel.Debug:
//            Console.WriteLine(message);
//            break;
//        case LogLevel.Error:
//            Console.WriteLine(message);
//            break;
//        case LogLevel.Warn:
//            Console.WriteLine(message);
//            break;
//        default:
//            Console.WriteLine(message);
//            break;
//    }
//};
//server.Start(socket =>
//{
//    socket.OnOpen = () =>
//    {
//        Console.WriteLine("Open");
//        socket.Send(MinecraftJsonFormat.subscribe("PlayerMessage"));
//    };
//    socket.OnClose = () =>
//    {
//        Console.WriteLine("Close");
//    };
//    socket.OnMessage = (msg) =>
//    {
//        Console.WriteLine("Message");
//        Console.WriteLine(msg);
//        socket.Send(msg);
//    };
//});