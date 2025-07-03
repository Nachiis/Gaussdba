using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
public enum MessageType
{
    // 服务请求
    None = 0,
    Login = 100,
    Register,
    LoadCuisine,
    Address,

    // 状态
    Error = 400,
    Success,
    Exception
}
[Serializable]
public class Message
{
    [JsonInclude]
    public MessageType type;
    [JsonInclude]
    public MessageType status;
    [JsonInclude]
    public string? content;
    [JsonInclude]
    public string? jsons;

    public string Tojson()
    {
        return JsonSerializer.Serialize(this);
    }


    public static T? FromJson<T>(string json)
    {
        return JsonSerializer.Deserialize<T>(json);
    }
}

[Serializable]
public class UsersMessage
{
    public string? username { get; set; }
    public string? password { get; set; }
    public short role { get; set; }
}