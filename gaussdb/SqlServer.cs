using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.WebSockets;
using System.Net;
using System.Net.Sockets;

namespace Gaussdb
{
    public class SqlServer : Singleton<SqlServer>
    {
        static HttpListener? httpListener;
        static int port = 8001;
        static readonly List<WebSocket> webSockets = new List<WebSocket>();
        static readonly object webSocketLock = new object();
        static bool isStop;
        public static async Task Server()
        {
            httpListener = new HttpListener();
            httpListener.Prefixes.Add($"http://localhost:{port}/");
            try
            {
                httpListener.Start();
                Utils.PrintInfo($"WebSocket server started on ws://localhost:{port}/");
                Utils.PrintInfo($"Press Ctrl+C to stop the server on port {port}.");
                Console.CancelKeyPress += (sender, e) =>
                {
                    e.Cancel = true;
                    isStop = true;
                    httpListener?.Stop();
                    
                };
                while (httpListener.IsListening)
                {
                    // 等待客户端连接 当服务端停止时，此处会抛出异常
                    var context = await httpListener.GetContextAsync();
                    if (context.Request.IsWebSocketRequest)
                    {
                        var webSocketContext = await context.AcceptWebSocketAsync(null);
                        var webSocket = webSocketContext.WebSocket;
                        var clientId = Guid.NewGuid().ToString();
                        lock (webSocketLock)
                        {
                            webSockets.Add(webSocket);
                        }
                        Utils.PrintInfo($"Client {clientId} connected from {context.Request.RemoteEndPoint?.Address}:{context.Request.RemoteEndPoint?.Port}");
                        // 启动处理客户端连接的任务
                        _ = HandleClientConnection(webSocket, clientId).ContinueWith(t =>
                        {
                            if (t.IsFaulted)
                            {
                                Utils.PrintError($"client {clientId}:{t.Exception?.GetBaseException().Message}");
                            }
                        });
                    }
                }
            }
            catch (HttpListenerException ex) when (ex.ErrorCode == 995)
            {
                if (!isStop)
                {
                    Utils.PrintError(ex);
                }
            }
            catch (Exception e)
            {
                Utils.PrintError(e);
            }
            finally
            {
                lock (webSocketLock)
                {
                    foreach (var webSocket in webSockets)
                    {
                        if (webSocket.State == WebSocketState.Open)
                        {
                            try
                            {
                                webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Server shutting down", System.Threading.CancellationToken.None).Wait();
                            }
                            catch (Exception ex)
                            {
                                Utils.PrintError($"Error closing WebSocket: {ex.Message}");
                            }
                        }
                    }
                    webSockets.Clear();
                }
                httpListener?.Close();
                Utils.PrintInfo($"WebSocket server stopped on port {port}.");
            }
        }
        private static async Task HandleClientConnection(WebSocket webSocket, string clientID)
        {
            var buffer = new byte[1024 * 16]; // buffer
            var receivedData = new List<byte>();
            try
            {
                while (webSocket.State == WebSocketState.Open)
                {
                    var result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), System.Threading.CancellationToken.None);
                    if (result.MessageType == WebSocketMessageType.Close)
                    {
                        await webSocket.CloseAsync(
                            WebSocketCloseStatus.NormalClosure,
                            "Closed by client",
                            CancellationToken.None);
                        break;
                    }

                    if (result.Count > 0)
                    {
                        receivedData.AddRange(buffer.Take(result.Count));
                    }

                    if (result.EndOfMessage && receivedData.Count > 0)
                    {
                        // 处理消息
                        await ProcessMessage(receivedData.ToArray(), result.MessageType, webSocket, clientID);
                        receivedData.Clear();
                    }

                }
            }
            catch (OperationCanceledException)
            {
                // 服务器端主动关闭连接
            }
            catch (WebSocketException e) when (e.WebSocketErrorCode == WebSocketError.ConnectionClosedPrematurely)
            {
                Utils.PrintWarning($"Client {clientID} disconnected unexpectedly: {e.Message}");
            }
            catch (Exception e)
            {
                Utils.PrintError($"Handling client {clientID}: {e.Message}");
            }
            finally
            {
                // 关闭 WebSocket 连接 
                lock (webSocketLock)
                {
                    webSockets.Remove(webSocket);
                }
                if (webSocket.State == WebSocketState.Open)
                {
                    try
                    {
                        await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Client disconnected", System.Threading.CancellationToken.None);
                    }
                    catch (Exception ex)
                    {
                        Utils.PrintError($"Error closing WebSocket for client {clientID}: {ex.Message}");
                    }
                }
                Utils.PrintInfo($"Client {clientID} disconnected from {webSocket.CloseStatusDescription}.");
            }
        }

        private static async Task ProcessMessage(byte[] bytes, WebSocketMessageType messageType, WebSocket webSocket, string clientID)
        {
            Utils.PrintInfo($"Received message from {clientID}: {Encoding.UTF8.GetString(bytes)}");
            if (messageType == WebSocketMessageType.Text)
            {
                // Json
                string json = Encoding.UTF8.GetString(bytes);
                await ProcessTextMessage(json, webSocket, clientID);
            }
                
        }

        private static async Task ProcessTextMessage(string json, WebSocket webSocket, string clientID)
        {
            var message = Message.FromJson<Message>(json);
            // 理论上不可能会是 null
            if (message == null)
            {
                Utils.PrintError($"Invalid message format from {clientID}: {json}");
                return;
            }
            Utils.PrintInfo($"Processing message from {clientID}: {message.type} - {message.status}");
            // 处理消息
            await MessageProcesser.Instance.ProcessMessageAsync(webSocket, clientID, message);
        }
        /// <summary>
        /// 发送文本消息到指定的 WebSocket 客户端
        /// </summary>
        /// <param name="message"></param>
        /// <param name="webSocket"></param>
        /// <param name="clientID"></param>
        /// <returns></returns>
        public async Task SendText(string message, WebSocket webSocket)
        {
            if (httpListener == null || !httpListener.IsListening)
            {
                Utils.PrintError("WebSocket server is not running.");
                return;
            }
            if (webSocket.State != WebSocketState.Open)
            {
                lock (webSocketLock)
                {
                    webSockets.Remove(webSocket);
                }
                return;
            }
            byte[] messageBytes = Encoding.UTF8.GetBytes(message);
            try
            {
                await webSocket.SendAsync(
                    new ArraySegment<byte>(messageBytes),
                    WebSocketMessageType.Text,
                    true,
                    System.Threading.CancellationToken.None);
            }
            catch (Exception e)
            {
                Utils.PrintError(e);
            }
        }
    }
}
