using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Threading.Tasks;

namespace Gaussdb
{
    // 使用发布订阅的方式处理消息
    public class MessageProcesser : Singleton<MessageProcesser>
    {
        public delegate Task MessageHandlerDelegate(WebSocket webSocket, string clientId, Message message);
        private  Dictionary<MessageType, MessageHandlerDelegate> messageHandlers = new Dictionary<MessageType, MessageHandlerDelegate>();

        // 默认错误处理
        private Task defaultHandler(WebSocket webSocket, string clientId, Message message)
        {
            Utils.PrintError($"No handler registered for message type: {message.type}");
            var errorMessage = new Message
            {
                type = MessageType.Error,
                content = $"No handler registered for message type: {message.type}"
            };
            // 发送错误消息给客户端
            if (webSocket.State == WebSocketState.Open)
            {
                var errorJson = errorMessage.Tojson();
                var errorBytes = Encoding.UTF8.GetBytes(errorJson);
                return webSocket.SendAsync(new ArraySegment<byte>(errorBytes), WebSocketMessageType.Text, true, CancellationToken.None);
            }
            else
            {
                Utils.PrintInfo($"WebSocket is not open for client {clientId}. Cannot send error message.");
            }
            return Task.CompletedTask;
        }

        public void Init()
        {
            RegisterHandler(MessageType.Login, OnLogin);
            RegisterHandler(MessageType.Register, OnRegister);



        }

        public void RegisterHandler(MessageType type, MessageHandlerDelegate handler)
        {
            if (!messageHandlers.ContainsKey(type))
            {
                messageHandlers[type] = handler;
            }
            else
            {
                messageHandlers[type] += handler; // 支持多个处理器
            }
        }
        public async Task ProcessMessageAsync(WebSocket webSocket, string clientId, Message message)
        {
            if (messageHandlers.TryGetValue(message.type, out var handler))
            {
                await handler(webSocket, clientId, message);
            }
            else
            {
                await defaultHandler(webSocket, clientId, message);
            }
        }
        private async Task OnLogin(WebSocket webSocket, string clientId, Message message)
        {
            Utils.PrintInfo($"Client {clientId} is logging");
            if (string.IsNullOrEmpty(message.jsons))
            {
                Utils.PrintError("Login message does not contain jsons.");
                return;
            }
            var usersMessage = Message.FromJson<UsersMessage>(message.jsons);
            // 查找用户
            
        }
        private async Task OnRegister(WebSocket webSocket, string clientId, Message message)
        {
            Utils.PrintInfo($"Client {clientId} is registering");
            if (string.IsNullOrEmpty(message.jsons))
            {
                Utils.PrintError("Register message does not contain jsons.");
                return;
            }
            // 获取 UsersMessage 对象
            var usersMessage = Message.FromJson<UsersMessage>(message.jsons);

            if (usersMessage == null)
            {
                Utils.PrintError("Failed to parse UsersMessage from jsons.");
                return;
            }

            if (SqlSugar.Instance.db != null)
            {
                var existingUser = await SqlSugar.Instance.db.Queryable<yangyw_design_users>()
                    .Where(u => u.yyw_username == usersMessage.username)
                    .FirstAsync();
                if (existingUser != null) // 已经存在用户
                {
                    Utils.PrintInfo($"User {usersMessage.username} already exists.");
                    // 向客户端发送未成功消息
                    var errorMessage = new Message
                    {
                        type = MessageType.Register,
                        status = MessageType.Exception,
                        content = "User already exists."
                    };
                    string json = errorMessage.Tojson();
                    await SqlServer.Instance.SendText(json, webSocket);
                }
                else
                {
                    // 创建新用户
                    var newUser = new yangyw_design_users
                    {
                        yyw_username = usersMessage.username,
                        yyw_password = usersMessage.password,
                        yyw_role = usersMessage.role
                    };
                    try
                    {
                        await SqlSugar.Instance.db.Insertable(newUser).ExecuteCommandAsync();
                        Utils.PrintInfo($"User {usersMessage.username} registered successfully.");
                        // 向客户端发送成功消息
                        var successMessage = new Message
                        {
                            type = MessageType.Register,
                            status = MessageType.Success,
                            content = "Registration successful."
                        };
                        string json = successMessage.Tojson();
                        await SqlServer.Instance.SendText(json, webSocket);
                    }
                    catch (Exception ex)
                    {
                        Utils.PrintError($"Error registering user {usersMessage.username}: {ex.Message}");
                        // 向客户端发送错误消息
                        var errorMessage = new Message
                        {
                            type = MessageType.Register,
                            status = MessageType.Error,
                            content = "Registration failed."
                        };
                        string json = errorMessage.Tojson();
                        await SqlServer.Instance.SendText(json, webSocket);
                    }
                }
            }
        }
    }
}
