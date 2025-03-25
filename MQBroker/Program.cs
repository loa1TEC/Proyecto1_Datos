
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using MQClient;

class Program
{
    private static Dictionary<string, List<TcpClient>> subscribers = new Dictionary<string, List<TcpClient>>();
    private static Dictionary<TcpClient, Dictionary<string, Queue>> clientMessageQueues = new Dictionary<TcpClient, Dictionary<string, Queue>>();

    static void Main()
    {
        TcpListener server = new TcpListener(IPAddress.Any, 5000);
        server.Server.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
        server.Start();
        Console.WriteLine("MQBroker esperando conexiones en el puerto 5000...");

        while (true)
        {
            TcpClient client = server.AcceptTcpClient();
            Console.WriteLine($"🟢 Nueva conexión: {client.Client.RemoteEndPoint}");
            Task.Run(() => HandleClient(client));
        }
    }

    private static async void HandleClient(TcpClient client)
    {
        try
        {
            using (NetworkStream stream = client.GetStream())
            {
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
        }
        catch (Exception ex)
        {
            Console.WriteLine($"🔴 Error: {ex.Message}");
        }
        finally
        {
            client.Close();
            Console.WriteLine($"⚫ Conexión cerrada: {client.Client.RemoteEndPoint}");
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
                Publish(topic, parts[3]);
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
            subscribers[topic] = new List<TcpClient>();

        if (!subscribers[topic].Contains(client))
        {
            subscribers[topic].Add(client);
            Console.WriteLine($"➕ Cliente {appId} suscrito a {topic}");
        }

        if (!clientMessageQueues.ContainsKey(client))
            clientMessageQueues[client] = new Dictionary<string, Queue>();

        if (!clientMessageQueues[client].ContainsKey(topic))
            clientMessageQueues[client][topic] = new Queue();
    }

    private static void Unsubscribe(TcpClient client, string appId, string topic)
    {
        if (subscribers.ContainsKey(topic) && subscribers[topic].Remove(client))
        {
            Console.WriteLine($"➖ Cliente {appId} desuscrito de {topic}");
            if (subscribers[topic].Count == 0)
                subscribers.Remove(topic);
        }

        if (clientMessageQueues.TryGetValue(client, out var topics) && topics.Remove(topic))
            Console.WriteLine($"🗑️ Cola eliminada para {topic}");
    }

    private static void Publish(string topic, string message)
    {
        if (subscribers.TryGetValue(topic, out var clients))
        {
            foreach (var client in clients)
            {
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