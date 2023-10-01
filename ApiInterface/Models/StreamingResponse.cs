using System;
using System.Text.Json.Serialization;

namespace Tinkoff.Trading.OpenApi.Legacy.Models;

public abstract class StreamingResponse
{
    public abstract string Event { get; }

    public DateTime Time { get; protected set; }
}

public abstract class StreamingResponse<T> : StreamingResponse
{
    public T Payload { get; }

    protected StreamingResponse(T payload, DateTime time)
    {
        Payload = payload;
        Time = time;
    }
}