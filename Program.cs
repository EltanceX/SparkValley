using Newtonsoft.Json;
using WebSocketSharp.Server;
using WebSocketSharp;
using Newtonsoft.Json.Linq;
using System.Diagnostics;
using System.Collections.Generic;
using System;
using System.Linq;

namespace com.sparkValley
{
    class main
    {
        public static void Main()
        {
            DateTime startTime = DateTime.Now;
            Console.ForegroundColor = ConsoleColor.Cyan;
            logger.info("Spark Valley - Minecraft websocket server (Version 0.0) By EltanceX 2023");
            logger.info("Github: https://github.com/AquaDew/SparkValley");
            Console.ForegroundColor = ConsoleColor.Green;
            WSocket ws = new WSocket(3000);
            events.onPlayerMessage = (msg) =>
            {
                logger.warn($"My On Message Event! MSG: {msg}");
            };
            ws.Start();
            gameUtil util = new gameUtil(ws);
            //logger.info(gameUtil.uuidv4());
            //util.exec("say hi", (json) =>
            //{

            //});
            Console.ResetColor();
            logger.info($"Service started. ({(DateTime.Now - startTime).Milliseconds / 1000.0f}s)");
            logger.info("Type 'help' for help page.");




            while (true)
            {
                string input = Console.ReadLine();
                logger.info(">> " + input);
                //ws.sendall(input);
                //ws.sendall(MinecraftJsonFormat.commandRequest($"say §f{input}"));
                //Thread.Sleep(300);
                //ws.sendall(MinecraftJsonFormat.commandRequest($"testforblock ~~~ air 0"));
                util.exec($"say {input}", (json) =>
                {
                    logger.warn(json.ToString());
                });
                if (input != null) consoleTerminal.emit(input);
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
        public void exec(string cmd, cmdCallback cb = null)
        {
            var session = ws._socketServer.WebSocketServices.Hosts.ToList()[0].Sessions;
            if (session.Count == 0)
            {
                logger.warn($"Trying to send command '{cmd}', but there are no sessions in the current list");
                logger.debug("Event 'exec' was cancelled.");
                return;
            }
            if (cb == null)
            {
                ws.sendall(MinecraftJsonFormat.commandRequest(cmd));
                return;
            }
            string uuid = uuidv4();
            cmdQueue.Add(uuid, cb);
            ws.sendall(MinecraftJsonFormat.commandRequest(cmd, uuid));
        }
    }
    public class events
    {
        public delegate void opm(string msg);
        public static opm onPlayerMessage;
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
            logger.info($"SparkValley Websocket Server listening on port {port}");
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
                logger.debug($"Sending data <String>\"{data}\" to <WS.Client.ID>\"{sid}\"");
                session.SendTo(data, sid);
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
            logger.info("A connection Closed. Current number of connections: " + Sessions.Count);
        }
        protected override void OnOpen()
        {
            logger.info("A connection Established. Current number of connections: " + Sessions.Count);
            this.Send(MinecraftJsonFormat.subscribe());
        }
        protected override void OnError(WebSocketSharp.ErrorEventArgs e)
        {
            logger.error("An Error occurred ! [at Websocket.OnError]");
            logger.error("Error Data: " + e.Message);
        }
        protected override void OnMessage(MessageEventArgs e)
        {
            string data = e.Data;
            JObject json;
            try
            {
                json = (JObject)JsonConvert.DeserializeObject(data);
                if (json == null) return;
            }
            catch (JsonReaderException ex)
            {
                logger.error(ex);
                return;
            }
            if (json["header"]?["messagePurpose"]?.ToString() == "commandResponse")
            {
                logger.info("Command Response.");
                string retUUID = json["header"]?["requestId"]?.ToString();
                if (retUUID == null) return;
                //Debugger.Break();
                if (gameUtil.cmdQueue.ContainsKey(retUUID))
                {
                    gameUtil.cmdQueue[retUUID].Invoke(json);
                    gameUtil.cmdQueue.Remove(retUUID);
                    //Debugger.Break();
                }
            }
            else if (json["body"]?["type"]?.ToString() == "chat")
            {
                string message = json["body"]["message"].ToString();
                logger.info($"<{json["body"]["sender"]}> {message}");
                events.onPlayerMessage?.Invoke(message);
            }
            logger.info(json);
        }
        //public bool SendDataEvent(string sData)
        //{
        //    if (State == WebSocketState.Open)
        //    {
        //        if (Sessions.Count > 0)
        //        {
        //            Send(sData);
        //            return true;
        //        }
        //    }
        //    return false;
        //}
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
        public static string commandRequest(string cmd, string uuid = null)
        {
            if (uuid == null) uuid = gameUtil.uuidv4();
            return @"{
    ""body"": {
        ""origin"": {
            ""type"": ""player""
        },
        ""commandLine"": """ + cmd + @""",
        ""version"": 1
    },
    ""header"": {
        ""requestId"": """ + uuid + @""",
        ""messagePurpose"": ""commandRequest"",
        ""version"": 1,
        ""messageType"": ""commandRequest""
    }
}";
        }
    }
    class consoleTerminal
    {
        public static void emit(string str)
        {

        }
    }
    class logger
    {
        static string date()
        {
            return DateTime.Now.ToString("[yyyy-MM-dd HH:mm:ss.fff]");
        }
        public static void info(string str)
        {
            Console.WriteLine($"[Info]{date()} {str}");
        }
        public static void info(string str, Exception ex)
        {
            Console.WriteLine($"[Info]{date()} {str} {ex}");
        }
        public static void info(object obj)
        {
            Console.WriteLine($"[Info]{date()} {JsonConvert.SerializeObject(obj)}");
        }
        public static void debug(string str)
        {
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.WriteLine($"[Debug]{date()} {str}");
            Console.ResetColor();
        }
        public static void error(Exception e)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"[Error]{date()} {e}");
            Console.ResetColor();
        }
        public static void error(string str)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"[Error]{date()} {str}");
            Console.ResetColor();
        }
        public static void error(string str, Exception e)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"[Error]{date()} {str} {e}");
            Console.ResetColor();
        }
        public static void warn(string str)
        {
            Console.ForegroundColor = ConsoleColor.DarkYellow;
            Console.WriteLine($"[Warn]{date()} {str}");
            Console.ResetColor();
        }
        public static void warn(string str, Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.DarkYellow;
            Console.WriteLine($"[Warn]{date()} {str} {ex}");
            Console.ResetColor();
        }
    }

}

//var server = new WebSocketServer("ws://127.0.0.1:3000");
//FleckLog.LogAction = (level, message, ex) =>
//{
//    switch (level)
//    {
//        case LogLevel.Debug:
//            logger.info(message, ex);
//            break;
//        case LogLevel.Error:
//            logger.error(message, ex);
//            break;
//        case LogLevel.Warn:
//            logger.warn(message, ex);
//            break;
//        default:
//            logger.info(message, ex);
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