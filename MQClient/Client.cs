using System;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace MQClient
{
    public class Client
    {
        private string serverIp = "127.0.0.1";
        private int serverPort = 5000;

        public async Task<string> SendMessageAsync(string command, string topic, string message = "")
        {
            try
            {
                using (TcpClient client = new TcpClient())
                {
                    await client.ConnectAsync(serverIp, serverPort);
                    NetworkStream stream = client.GetStream();

                    string fullMessage = $"{command}|{topic}|{message}";
                    byte[] data = Encoding.UTF8.GetBytes(fullMessage);
                    await stream.WriteAsync(data, 0, data.Length);

                    // Recibir la respuesta del Broker
                    byte[] responseBuffer = new byte[1024];
                    int bytesRead = await stream.ReadAsync(responseBuffer, 0, responseBuffer.Length);
                    return Encoding.UTF8.GetString(responseBuffer, 0, bytesRead); // Retornar la respuesta
                }
            }
            catch (Exception ex)
            {
                return $"Error: {ex.Message}";
            }
        }



    }
}
