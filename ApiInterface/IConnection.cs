namespace Tinkoff.Trading.OpenApi.Legacy.Network;

/// <summary>
/// Интерфейс подключения к API брокера/источника.
/// </summary>
public interface IConnection : IDisposable, IContext
{
    /// <summary>
    /// Состояние соединения (установлено или нет).
    /// </summary>
    bool IsAlive { get; }
}

public interface ISandboxConnection : IConnection, ISandboxContext
{
    
}