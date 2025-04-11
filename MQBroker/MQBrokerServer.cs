using System;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace MQBroker
{
    public class MQBrokerServer
    {
        private TcpListener? server;
        private bool isRunning;
        private SubscriptionManager subscriptionManager;

        public MQBrokerServer()
        {
            // Inicialización del SubscriptionManager y del listener en el puerto 5000.
            subscriptionManager = new SubscriptionManager();
            server = new TcpListener(IPAddress.Any, 5000);
            server.Server.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            isRunning = true;
        }

        public void Start()
        {
            if (server == null)
            {
                throw new InvalidOperationException("The server has not been initialized.");
            }

            server.Start();
            Console.WriteLine("🚀 MQBroker iniciado en el puerto 5000...");

            // Manejo para cerrar el servidor de forma segura con Ctrl+C.
            Console.CancelKeyPress += (sender, e) =>
            {
                Console.WriteLine("\n🔴 Cerrando el servidor de manera segura...");
                isRunning = false;
                server?.Stop();
                Environment.Exit(0);
            };

            // Bucle principal para aceptar conexiones.
            while (isRunning)
            {
                try
                {
                    TcpClient client = server.AcceptTcpClient();
                    if (client != null)
                    {
                        Console.WriteLine($"🟢 Nueva conexión: {client.Client.RemoteEndPoint}");
                        // Se lanza un Task por cada cliente para procesarlo de forma asíncrona.
                        Task.Run(() => new ClientHandler(client, subscriptionManager).ProcessAsync());
                    }
                }
                catch (SocketException)
                {
                    if (!isRunning) break;
                }
            }
        }
    }
}
