using System;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace MQBroker
{
    public class ClientHandler
    {
        private readonly TcpClient client;
        private readonly SubscriptionManager subscriptionManager;

        public ClientHandler(TcpClient client, SubscriptionManager subscriptionManager)
        {
            this.client = client;
            this.subscriptionManager = subscriptionManager;
        }

        public async Task ProcessAsync()
        {
            try
            {
                NetworkStream stream = client.GetStream();

                // Se procesa el flujo de datos mientras el cliente esté conectado.
                while (client.Connected)
                {
                    // 1. Leer la cabecera: 4 bytes que indican la longitud del mensaje.
                    byte[] lengthBuffer = new byte[4];
                    int bytesRead = await ReadExactAsync(stream, lengthBuffer, 4);
                    if (bytesRead == 0)
                        break; // Se cerró la conexión.

                    // Convertir los 4 bytes a un entero para obtener la longitud del mensaje.
                    int messageLength = BitConverter.ToInt32(lengthBuffer, 0);
                    if (messageLength <= 0)
                        continue; // Valor inválido, se salta este ciclo.

                    // 2. Leer el mensaje completo según la longitud indicada.
                    byte[] messageBuffer = new byte[messageLength];
                    bytesRead = await ReadExactAsync(stream, messageBuffer, messageLength);
                    if (bytesRead == 0)
                        break; // Se cerró la conexión.

                    // Convertir el mensaje a cadena.
                    string message = Encoding.UTF8.GetString(messageBuffer);
                    Console.WriteLine($"📩 Mensaje recibido: {message}");

                    // Procesa el mensaje y genera una respuesta.
                    string response = ProcessMessage(message);

                    // Envía la respuesta utilizando el mismo protocolo: cabecera + mensaje.
                    await SendMessageAsync(stream, response);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"🔴 Error: {ex.Message}");
            }
            finally
            {
                DisconnectClient();
            }
        }

        /// <summary>
        /// Lee exactamente "size" bytes del stream y los almacena en buffer.
        /// </summary>
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
        /// Envía un mensaje con protocolo de cabecera (4 bytes de longitud + mensaje).
        /// </summary>
        private async Task SendMessageAsync(NetworkStream stream, string message)
        {
            byte[] messageBytes = Encoding.UTF8.GetBytes(message);
            byte[] lengthBytes = BitConverter.GetBytes(messageBytes.Length);

            // 1. Enviar la cabecera que contiene la longitud del mensaje.
            await stream.WriteAsync(lengthBytes, 0, lengthBytes.Length);
            // 2. Enviar el mensaje.
            await stream.WriteAsync(messageBytes, 0, messageBytes.Length);
        }

        /// <summary>
        /// Cierra la conexión con el cliente.
        /// </summary>
        private void DisconnectClient()
        {
            if (client.Connected)
            {
                Console.WriteLine($"⚫ Conexión cerrada: {client.Client.RemoteEndPoint}");
                client.Close();
            }
            else
            {
                Console.WriteLine("⚠️ Intento de cerrar un cliente ya desconectado.");
            }
        }

        /// <summary>
        /// Procesa el mensaje recibido delegando la lógica al SubscriptionManager.
        /// El formato del mensaje esperado es: comando|appId|topic|...
        /// </summary>
        private string ProcessMessage(string message)
        {
            string[] parts = message.Split('|');
            if (parts.Length < 3)
                return "ERROR: Formato inválido";

            string command = parts[0];
            string appId = parts[1];
            string topic = parts[2];

            switch (command)
            {
                case "SUBSCRIBE":
                    return subscriptionManager.Subscribe(client, appId, topic);
                case "UNSUBSCRIBE":
                    subscriptionManager.Unsubscribe(client, appId, topic);
                    return $"DESUSCRITO_DE|{topic}";
                case "PUBLISH":
                    if (parts.Length < 4)
                        return "ERROR: Falta mensaje";
                    subscriptionManager.Publish(topic, parts[3], client);
                    return $"PUBLICADO_EN|{topic}";
                case "RECEIVE":
                    return subscriptionManager.ReceiveMessage(client, appId, topic);
                default:
                    return "ERROR: Comando desconocido";
            }
        }
    }
}
