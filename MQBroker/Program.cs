using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using MQClient;

class Program
{
    private static CustomDictionary<string, List<TcpClient>> subscribers = new CustomDictionary<string, List<TcpClient>>();
    private static CustomDictionary<TcpClient, CustomDictionary<string, Queue>> clientMessageQueues = new CustomDictionary<TcpClient, CustomDictionary<string, Queue>>();

    private static TcpListener? server;

    private static bool isRunning = true;

    static void Main()
    {
        StartServer();
        Console.CancelKeyPress += (sender, e) =>
        {
            Console.WriteLine("\n🔴 Cerrando el servidor de manera segura...");
            isRunning = false;
            server?.Stop();
            Environment.Exit(0);
        };

        while (isRunning)
        {
            try
            {
                var client = server?.AcceptTcpClient();
                if (client != null)
                {
                    Console.WriteLine($"🟢 Nueva conexión: {client.Client.RemoteEndPoint}");
                    Task.Run(() => HandleClient(client));
                }
            }
            catch (SocketException)
            {
                if (!isRunning) break;
            }
        }
    }

    private static void StartServer()
    {
        server = new TcpListener(IPAddress.Any, 5000);
        server.Server.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
        server.Start();
        Console.WriteLine("🚀 MQBroker iniciado en el puerto 5000...");
    }

    private static async void HandleClient(TcpClient client)
    {
        try
        {
            NetworkStream stream = client.GetStream();
            byte[] buffer = new byte[1024];

            while (client.Connected)
            {
                int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
                if (bytesRead == 0) break;

                string message = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                Console.WriteLine($"📩 Mensaje recibido: {message}");

                string response = ProcessMessage(client, message);
                byte[] responseData = Encoding.UTF8.GetBytes(response);
                await stream.WriteAsync(responseData, 0, responseData.Length);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"🔴 Error: {ex.Message}");
        }
        finally
        {
            DisconnectClient(client);
        }
    }

    private static void DisconnectClient(TcpClient client)
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

    private static string ProcessMessage(TcpClient client, string message)
    {
        string[] parts = message.Split('|');
        if (parts.Length < 3) return "ERROR: Formato inválido";

        string command = parts[0];
        string appId = parts[1];
        string topic = parts[2];

        switch (command)
        {
            case "SUBSCRIBE":
                Subscribe(client, appId, topic);
                return $"SUSCRITO_A|{topic}";
            case "UNSUBSCRIBE":
                Unsubscribe(client, appId, topic);
                return $"DESUSCRITO_DE|{topic}";
            case "PUBLISH":
                if (parts.Length < 4) return "ERROR: Falta mensaje";
                Publish(topic, parts[3], client); // ← Pasamos 'client' para excluirlo
                return $"PUBLICADO_EN|{topic}";
            case "RECEIVE":
                return ReceiveMessage(client, appId, topic);
            default:
                return "ERROR: Comando desconocido";
        }
    }

    private static void Subscribe(TcpClient client, string appId, string topic)
    {
        if (!subscribers.ContainsKey(topic))
            subscribers.Add(topic, new List<TcpClient>());

        if (subscribers.TryGetValue(topic, out var clients) && !clients.Contains(client))
        {
            clients.Add(client);
            Console.WriteLine($"➕ Cliente {appId} suscrito a {topic}");
        }

        if (!clientMessageQueues.ContainsKey(client))
            clientMessageQueues.Add(client, new CustomDictionary<string, Queue>());

        if (clientMessageQueues.TryGetValue(client, out var topics) && !topics.ContainsKey(topic))
            topics.Add(topic, new Queue());
    }

    private static void Unsubscribe(TcpClient client, string appId, string topic)
    {
        if (subscribers.TryGetValue(topic, out var clients) && clients.Remove(client))
        {
            Console.WriteLine($"➖ Cliente {appId} desuscrito de {topic}");
            if (clients.Count == 0)
                subscribers.Remove(topic);
        }

        if (clientMessageQueues.TryGetValue(client, out var topics))
            topics.Remove(topic);
    }

    private static void Publish(string topic, string message, TcpClient sender)
    {
        if (subscribers.TryGetValue(topic, out var clients))
        {
            foreach (var client in clients)
            {
                if (client == sender) continue; // Evita que el emisor reciba su propio mensaje

                if (clientMessageQueues.TryGetValue(client, out var topics) && topics.TryGetValue(topic, out var queue))
                {
                    queue.Enqueue(new Nodo(message));
                    Console.WriteLine($"📤 Mensaje publicado en {topic} para {client.Client.RemoteEndPoint}");
                }
            }
        }
    }

    private static string ReceiveMessage(TcpClient client, string appId, string topic)
    {
        if (clientMessageQueues.TryGetValue(client, out var topics) &&
            topics.TryGetValue(topic, out var queue) &&
            !queue.IsEmpty())
        {
            return queue.Dequeue().Data;
        }
        return "EMPTY";
    }
}
