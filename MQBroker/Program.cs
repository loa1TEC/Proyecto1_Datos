using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using MQClient;

/// <summary>
/// Clase principal del servidor MQBroker.
/// Gestiona suscripciones, publicación y entrega de mensajes entre clientes TCP.
/// </summary>
class Program
{
    /// <summary>
    /// Diccionario de tópicos con sus listas de clientes suscritos.
    /// </summary>
    private static CustomDictionary<string, List<TcpClient>> subscribers = new CustomDictionary<string, List<TcpClient>>();

    /// <summary>
    /// Diccionario de clientes con sus colas de mensajes por tópico.
    /// </summary>
    private static CustomDictionary<TcpClient, CustomDictionary<string, Queue>> clientMessageQueues = new CustomDictionary<TcpClient, CustomDictionary<string, Queue>>();

    /// <summary>
    /// Instancia del servidor TCP.
    /// </summary>
    private static TcpListener? server;

    /// <summary>
    /// Indica si el servidor está en ejecución.
    /// </summary>
    private static bool isRunning = true;

    /// <summary>
    /// Punto de entrada principal del programa. Inicia el servidor y gestiona conexiones.
    /// </summary>
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

    /// <summary>
    /// Inicializa el servidor TCP en el puerto 5000.
    /// </summary>
    private static void StartServer()
    {
        server = new TcpListener(IPAddress.Any, 5000);
        server.Server.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
        server.Start();
        Console.WriteLine("🚀 MQBroker iniciado en el puerto 5000...");
    }

    /// <summary>
    /// Maneja la conexión y comunicación con un cliente individual.
    /// </summary>
    /// <param name="client">Cliente TCP conectado.</param>
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

    /// <summary>
    /// Cierra la conexión con un cliente.
    /// </summary>
    /// <param name="client">Cliente a desconectar.</param>
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

    /// <summary>
    /// Procesa un mensaje recibido desde un cliente.
    /// </summary>
    /// <param name="client">Cliente que envió el mensaje.</param>
    /// <param name="message">Mensaje recibido.</param>
    /// <returns>Respuesta al cliente.</returns>
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
                return Subscribe(client, appId, topic);
            case "UNSUBSCRIBE":
                Unsubscribe(client, appId, topic);
                return $"DESUSCRITO_DE|{topic}";
            case "PUBLISH":
                if (parts.Length < 4) return "ERROR: Falta mensaje";
                Publish(topic, parts[3], client);
                return $"PUBLICADO_EN|{topic}";
            case "RECEIVE":
                return ReceiveMessage(client, appId, topic);
            default:
                return "ERROR: Comando desconocido";
        }
    }

    /// <summary>
    /// Registra a un cliente en un tópico determinado.
    /// </summary>
    /// <param name="client">Cliente TCP.</param>
    /// <param name="appId">ID de la aplicación.</param>
    /// <param name="topic">Tópico de suscripción.</param>
    /// <returns>Confirmación de suscripción.</returns>
    private static string Subscribe(TcpClient client, string appId, string topic)
    {
        if (!subscribers.ContainsKey(topic))
            subscribers.Add(topic, new List<TcpClient>());

        if (subscribers.TryGetValue(topic, out var clients))
        {
            if (clients.Contains(client))
            {
                Console.WriteLine($"⚠️ Cliente {appId} ya está suscrito a {topic}");
                return $"YA_SUSCRITO_A|{topic}";
            }

            clients.Add(client);
            Console.WriteLine($"➕ Cliente {appId} suscrito a {topic}");
        }

        if (!clientMessageQueues.ContainsKey(client))
            clientMessageQueues.Add(client, new CustomDictionary<string, Queue>());

        if (clientMessageQueues.TryGetValue(client, out var topics) && !topics.ContainsKey(topic))
            topics.Add(topic, new Queue());

        return $"SUSCRITO_A|{topic}";
    }

    /// <summary>
    /// Elimina la suscripción de un cliente a un tópico.
    /// </summary>
    /// <param name="client">Cliente TCP.</param>
    /// <param name="appId">ID de la aplicación.</param>
    /// <param name="topic">Tópico a desuscribir.</param>
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

    /// <summary>
    /// Publica un mensaje en un tópico a todos los suscriptores (excepto el emisor).
    /// </summary>
    /// <param name="topic">Tópico destino.</param>
    /// <param name="message">Mensaje a enviar.</param>
    /// <param name="sender">Cliente que envió el mensaje.</param>
    private static void Publish(string topic, string message, TcpClient sender)
    {
        if (subscribers.TryGetValue(topic, out var clients))
        {
            foreach (var client in clients)
            {
                if (client == sender) continue;

                if (clientMessageQueues.TryGetValue(client, out var topics) &&
                    topics.TryGetValue(topic, out var queue))
                {
                    queue.Enqueue(new Nodo(message));
                    Console.WriteLine($"📤 Mensaje publicado en {topic} para {client.Client.RemoteEndPoint}");
                }
            }
        }
    }

    /// <summary>
    /// Recupera el siguiente mensaje en la cola del cliente para un tópico específico.
    /// </summary>
    /// <param name="client">Cliente TCP.</param>
    /// <param name="appId">ID de la aplicación.</param>
    /// <param name="topic">Tópico del que recibir mensajes.</param>
    /// <returns>Mensaje o "EMPTY" si no hay mensajes.</returns>
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
