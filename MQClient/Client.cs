using System;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace MQClient
{
    /// <summary>
    /// Cliente para interactuar con el servidor MQBroker.
    /// Permite suscribirse, publicar y recibir mensajes por tópicos.
    /// </summary>
    public class Client : IDisposable
    {
        private TcpClient _tcpClient;
        private NetworkStream _stream;
        private readonly string _appId;
        private readonly string _serverIp;
        private readonly int _serverPort;

        /// <summary>
        /// Inicializa una nueva instancia del cliente MQ.
        /// </summary>
        /// <param name="appId">ID único de la aplicación cliente.</param>
        /// <param name="serverIp">Dirección IP del servidor.</param>
        /// <param name="port">Puerto de conexión al servidor.</param>
        public Client(string appId, string serverIp, int port = 5000)
        {
            _appId = appId;
            _serverIp = serverIp;
            _serverPort = port;
            InitializeConnection();
        }

        /// <summary>
        /// Establece la conexión con el servidor.
        /// </summary>
        private void InitializeConnection()
        {
            try
            {
                _tcpClient = new TcpClient();
                _tcpClient.Connect(_serverIp, _serverPort);
                _stream = _tcpClient.GetStream();
                Console.WriteLine($"✅ Conexión establecida con {_serverIp}:{_serverPort}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"🔴 Error de conexión: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Envía un mensaje utilizando el nuevo protocolo: cabecera de 4 bytes + mensaje.
        /// Espera la respuesta del servidor formateada de la misma manera.
        /// </summary>
        /// <param name="command">Comando a ejecutar (SUBSCRIBE, PUBLISH, etc.).</param>
        /// <param name="topic">Tópico relacionado con la operación.</param>
        /// <param name="message">Mensaje opcional (solo para publicar).</param>
        /// <returns>Respuesta del servidor.</returns>
        private async Task<string> SendMessageAsync(string command, Topic topic, string message = "")
        {
            try
            {
                if (!_tcpClient.Connected)
                {
                    Console.WriteLine("⚠️ Reconectando...");
                    InitializeConnection();
                }

                // Formatear el mensaje completo
                string fullMessage = $"{command}|{_appId}|{topic}|{message}";
                byte[] messageBytes = Encoding.UTF8.GetBytes(fullMessage);

                // Crear la cabecera con la longitud del mensaje
                byte[] lengthBytes = BitConverter.GetBytes(messageBytes.Length);

                // Enviar cabecera y mensaje
                await _stream.WriteAsync(lengthBytes, 0, lengthBytes.Length);
                await _stream.WriteAsync(messageBytes, 0, messageBytes.Length);

                // Leer la respuesta del servidor utilizando el mismo protocolo

                // 1. Leer la cabecera de 4 bytes de la respuesta
                byte[] responseLengthBuffer = new byte[4];
                int bytesRead = await ReadExactAsync(_stream, responseLengthBuffer, 4);
                if (bytesRead == 0)
                    return string.Empty;

                int responseLength = BitConverter.ToInt32(responseLengthBuffer, 0);

                // 2. Leer el mensaje completo de respuesta
                byte[] responseBuffer = new byte[responseLength];
                bytesRead = await ReadExactAsync(_stream, responseBuffer, responseLength);
                return Encoding.UTF8.GetString(responseBuffer, 0, bytesRead);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"🔴 Error en SendMessageAsync: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Lee exactamente "size" bytes del stream y los almacena en buffer.
        /// </summary>
        /// <param name="stream">NetworkStream del cual leer.</param>
        /// <param name="buffer">Buffer donde almacenar los datos leídos.</param>
        /// <param name="size">Cantidad de bytes a leer.</param>
        /// <returns>Número total de bytes leídos.</returns>
        private async Task<int> ReadExactAsync(NetworkStream stream, byte[] buffer, int size)
        {
            int totalBytesRead = 0;
            while (totalBytesRead < size)
            {
                int bytesRead = await stream.ReadAsync(buffer, totalBytesRead, size - totalBytesRead);
                if (bytesRead == 0)
                {
                    // La conexión se cerró.
                    break;
                }
                totalBytesRead += bytesRead;
            }
            return totalBytesRead;
        }

        /// <summary>
        /// Se suscribe a un tópico.
        /// </summary>
        /// <param name="topic">Tópico al que suscribirse.</param>
        /// <returns>True si la suscripción fue exitosa.</returns>
        public async Task<bool> Subscribe(Topic topic)
        {
            string response = await SendMessageAsync("SUBSCRIBE", topic);
            return response.StartsWith("SUSCRITO_A");
        }

        /// <summary>
        /// Cancela la suscripción a un tópico.
        /// </summary>
        /// <param name="topic">Tópico al que dejar de suscribirse.</param>
        /// <returns>True si se desuscribió correctamente.</returns>
        public async Task<bool> Unsubscribe(Topic topic)
        {
            string response = await SendMessageAsync("UNSUBSCRIBE", topic);
            return response.StartsWith("DESUSCRITO_DE");
        }

        /// <summary>
        /// Publica un mensaje en un tópico.
        /// </summary>
        /// <param name="message">Mensaje a publicar.</param>
        /// <param name="topic">Tópico de destino.</param>
        /// <returns>True si el mensaje fue publicado con éxito.</returns>
        public async Task<bool> Publish(Message message, Topic topic)
        {
            string response = await SendMessageAsync("PUBLISH", topic, message.ToString());
            return response.Contains("PUBLICADO_EN");
        }

        /// <summary>
        /// Solicita el siguiente mensaje disponible en el tópico.
        /// </summary>
        /// <param name="topic">Tópico del que recibir mensajes.</param>
        /// <returns>Mensaje recibido o null si no hay mensajes.</returns>
        public async Task<Message> Receive(Topic topic)
        {
            string response = await SendMessageAsync("RECEIVE", topic);
            if (response == "EMPTY")
                return null;
            return new Message(response);
        }

        /// <summary>
        /// Libera recursos y cierra la conexión con el servidor.
        /// </summary>
        public void Dispose()
        {
            _stream?.Close();
            _tcpClient?.Close();
            Console.WriteLine("⚫ Conexión cerrada");
        }
    }
}
