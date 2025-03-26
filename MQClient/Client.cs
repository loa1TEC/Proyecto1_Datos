using System;
using System.Linq;
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

        public Client(string appId, string ip = "127.0.0.1", int port = 5000)
        {
            _appId = appId;
            _serverIp = ip;
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

        public async Task<string> SendMessageAsync(string command, string topic, string message = "")
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
                return $"ERROR: {ex.Message}";
            }
        }

        public async Task<bool> Subscribe(string topic)
        {
            string response = await SendMessageAsync("SUBSCRIBE", topic);
            return response.StartsWith("SUSCRITO_A");
        }

        public async Task<bool> Unsubscribe(string topic)
        {
            string response = await SendMessageAsync("UNSUBSCRIBE", topic);
            return response.StartsWith("DESUSCRITO_DE");
        }

        public async Task<bool> Publish(string topic, string message)
        {
            string response = await SendMessageAsync("PUBLISH", topic, message);
            return response.Contains("publicado");
        }

        public async Task<string> Receive(string topic)
        {
            string response = await SendMessageAsync("RECEIVE", topic);
            return response.StartsWith("ERROR") ? response : response.Split('|').Last();
        }

        public void Dispose()
        {
            _stream?.Close();
            _tcpClient?.Close();
            Console.WriteLine("⚫ Conexión cerrada");
        }
    }
}