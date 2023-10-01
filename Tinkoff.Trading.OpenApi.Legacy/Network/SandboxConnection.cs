namespace Tinkoff.Trading.OpenApi.Legacy.Network;

public class SandboxConnection : Connection, ISandboxConnection
{
    public SandboxConnection(string token) : base(token, true, false)
    {
    }
    
    public SandboxConnection(string token, bool isStreaming) : base(token, true, isStreaming)
    {
    }
}