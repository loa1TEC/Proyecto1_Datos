using System;
using System.Net.Sockets;
using System.Text;

namespace MQClient
{
    public class Client
    {
        private string serverIp = "127.0.0.1";
        private int serverPort = 5000;

        public string SendMessage(string command, string topic, string message = "")
        {
            try
            {
                using (TcpClient client = new TcpClient(serverIp, serverPort))
                {
                    NetworkStream stream = client.GetStream();

                    // Formato: "COMMAND|TOPIC|MESSAGE"
                    string fullMessage = $"{command}|{topic}|{message}";
                    byte[] data = Encoding.UTF8.GetBytes(fullMessage);
                    stream.Write(data, 0, data.Length);

                    // Recibir respuesta del broker
                    byte[] buffer = new byte[1024];
                    int bytesRead = stream.Read(buffer, 0, buffer.Length);
                    string response = Encoding.UTF8.GetString(buffer, 0, bytesRead);

                    return response;
                }
            }
            catch (Exception ex)
            {
                return $"Error: {ex.Message}";
            }
        }
    }
}

