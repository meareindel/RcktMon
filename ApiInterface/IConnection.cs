using System;

namespace Tinkoff.Trading.OpenApi.Legacy.Network;

/// <summary>
/// Интерфейс подключения к API брокера/источника.
/// </summary>
public interface IConnection : IDisposable, IContext
{

}

public interface ISandboxConnection : IConnection, ISandboxContext
{
    
}