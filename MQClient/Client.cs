using System;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace MQClient
{
    public class Client : IDisposable
    {
        private TcpClient _tcpClient;
        private NetworkStream _stream;
        private readonly string _appId;
        private readonly string _serverIp;
        private readonly int _serverPort;

        public Client(string appId, string serverIp = "172.18.16.204", int port = 5000)
        {
            _appId = appId;
            _serverIp = serverIp;
            _serverPort = port;
            InitializeConnection();
        }

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

        private async Task<string> SendMessageAsync(string command, Topic topic, string message = "")
        {
            try
            {
                if (!_tcpClient.Connected)
                {
                    Console.WriteLine("⚠️ Reconectando...");
                    InitializeConnection();
                }

                string fullMessage = $"{command}|{_appId}|{topic}|{message}";
                byte[] data = Encoding.UTF8.GetBytes(fullMessage);
                await _stream.WriteAsync(data, 0, data.Length);

                byte[] buffer = new byte[1024];
                int bytesRead = await _stream.ReadAsync(buffer, 0, buffer.Length);
                return Encoding.UTF8.GetString(buffer, 0, bytesRead);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"🔴 Error en SendMessageAsync: {ex.Message}");
                throw;
            }
        }

        public async Task<bool> Subscribe(Topic topic)
        {
            string response = await SendMessageAsync("SUBSCRIBE", topic);
            return response.StartsWith("SUSCRITO_A");
        }

        public async Task<bool> Unsubscribe(Topic topic)
        {
            string response = await SendMessageAsync("UNSUBSCRIBE", topic);
            return response.StartsWith("DESUSCRITO_DE");
        }

        public async Task<bool> Publish(Message message, Topic topic)
        {
            string response = await SendMessageAsync("PUBLISH", topic, message.ToString());
            return response.Contains("PUBLICADO_EN");
        }

        public async Task<Message> Receive(Topic topic)
        {
            string response = await SendMessageAsync("RECEIVE", topic);
            if (response == "EMPTY") return null;
            return new Message(response);
        }

        public void Dispose()
        {
            _stream?.Close();
            _tcpClient?.Close();
            Console.WriteLine("⚫ Conexión cerrada");
        }
    }
}
