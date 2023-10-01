using System.Net.Http;

namespace Tinkoff.Trading.OpenApi.Legacy.Network
{
    public class ConnectionFactory
    {
        /// <summary>
        /// Создаёт объект подключения к OpenAPI.
        /// </summary>
        /// <param name="token">Токен аутентификации.</param>
        /// <returns>Подключение к бирже.</returns>
        public static Connection GetConnection(string token)
        {
            return new Connection(token);
        }

        public static Connection GetStreamingConnection(string token)
        {
            return new Connection(token, true);
        }
        
        /// <summary>
        /// Создаёт объект подключения к OpenAPI в режиме песочницы.
        /// </summary>
        /// <param name="token">Токен аутентификации.</param>
        /// <returns>Подключение к бирже в режиме песочницы.</returns>
        public static SandboxConnection GetSandboxConnection(string token)
        {
            return new SandboxConnection(token);
        }
    }
}