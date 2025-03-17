using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

class Program
{
    // Diccionarios para manejar suscriptores y colas de mensajes
    private static Dictionary<string, List<TcpClient>> subscribers = new Dictionary<string, List<TcpClient>>();
    private static Dictionary<TcpClient, Dictionary<string, Queue<string>>> clientMessageQueues = new Dictionary<TcpClient, Dictionary<string, Queue<string>>>();

    // Método principal
    static void Main()
    {
        TcpListener server = new TcpListener(IPAddress.Any, 5000);
        server.Start();
        Console.WriteLine("MQBroker esperando conexiones en el puerto 5000...");

        // Escucha conexiones entrantes
        while (true)
        {
            TcpClient client = server.AcceptTcpClient();
            Task.Run(() => HandleClient(client)); // Maneja el cliente en un hilo separado
        }
    }

    // Manejo de las solicitudes del cliente
    private static void HandleClient(TcpClient client)
    {
        NetworkStream stream = client.GetStream();
        byte[] buffer = new byte[1024];
        int bytesRead = stream.Read(buffer, 0, buffer.Length);
        string message = Encoding.UTF8.GetString(buffer, 0, bytesRead);

        string[] parts = message.Split('|');
        if (parts.Length < 2) return;

        string command = parts[0];
        string topic = parts[1];

        string response = ""; // Variable para la respuesta del broker

        switch (command)
        {
            case "SUBSCRIBE":
                Subscribe(client, topic);
                response = $"Te has suscrito al tema: {topic}";
                break;
            case "UNSUBSCRIBE":
                Unsubscribe(client, topic);
                response = $"Te has desuscrito del tema: {topic}";
                break;
            case "PUBLISH":
                if (parts.Length < 3) return;
                Publish(topic, parts[2]);
                response = $"Mensaje '{parts[2]}' publicado en el tema: {topic}";
                break;
            case "RECEIVE":
                response = ReceiveMessage(client, topic);
                break;
        }

        // Enviar la respuesta al cliente
        byte[] responseData = Encoding.UTF8.GetBytes(response);
        stream.Write(responseData, 0, responseData.Length);
    }


    // Procesar los comandos de los clientes
    private static void ProcessCommand(string command, string topic, string[] parts, TcpClient client, NetworkStream stream)
    {
        switch (command)
        {
            case "SUBSCRIBE":
                Subscribe(client, topic);
                break;
            case "UNSUBSCRIBE":
                Unsubscribe(client, topic);
                break;
            case "PUBLISH":
                if (parts.Length < 3) return;
                Publish(topic, parts[2]);
                break;
            case "RECEIVE":
                string receivedMessage = ReceiveMessage(client, topic);
                byte[] responseData = Encoding.UTF8.GetBytes(receivedMessage);
                stream.Write(responseData, 0, responseData.Length);
                break;
            default:
                Console.WriteLine("Comando desconocido");
                break;
        }
    }

    // Función para manejar la suscripción
    private static void Subscribe(TcpClient client, string topic)
    {
        if (!subscribers.ContainsKey(topic))
        {
            subscribers[topic] = new List<TcpClient>(); // Crear lista de suscriptores si no existe
            Console.WriteLine($"Creando lista de suscriptores para el tema: {topic}");
        }

        if (!subscribers[topic].Contains(client))
        {
            subscribers[topic].Add(client); // Agregar el cliente a la lista de suscriptores
            Console.WriteLine($"Cliente suscrito a {topic}");
        }

        // Asegúrate de que el cliente tenga una cola de mensajes para este tema
        if (!clientMessageQueues.ContainsKey(client))
        {
            clientMessageQueues[client] = new Dictionary<string, Queue<string>>();
        }

        // Crear la cola de mensajes para este tema
        if (!clientMessageQueues[client].ContainsKey(topic))
        {
            clientMessageQueues[client][topic] = new Queue<string>();
            Console.WriteLine($"Cola de mensajes creada para el cliente en el tema {topic}");
        }
    }



    // Función para manejar la desuscripción
    private static void Unsubscribe(TcpClient client, string topic)
    {
        if (subscribers.ContainsKey(topic))
        {
            subscribers[topic].Remove(client);
            if (subscribers[topic].Count == 0)
            {
                subscribers.Remove(topic);
            }
        }

        if (clientMessageQueues.ContainsKey(client) && clientMessageQueues[client].ContainsKey(topic))
        {
            clientMessageQueues[client].Remove(topic); // Eliminar la cola de mensajes del cliente
            Console.WriteLine($"Cliente desuscrito de {topic}, eliminando su cola de mensajes");
        }
    }

    // Función para publicar mensajes
    private static void Publish(string topic, string message)
    {
        if (subscribers.ContainsKey(topic))
        {
            foreach (TcpClient client in subscribers[topic])
            {
                if (!clientMessageQueues.ContainsKey(client))
                {
                    clientMessageQueues[client] = new Dictionary<string, Queue<string>>();
                }

                if (!clientMessageQueues[client].ContainsKey(topic))
                {
                    clientMessageQueues[client][topic] = new Queue<string>();
                }

                clientMessageQueues[client][topic].Enqueue(message); // Encolar el mensaje para cada cliente
                Console.WriteLine($"Mensaje '{message}' agregado a las colas de {topic} para el cliente.");
            }
        }
    }



    // Función para recibir mensajes
    private static string ReceiveMessage(TcpClient client, string topic)
    {
        if (!subscribers.ContainsKey(topic) || !subscribers[topic].Contains(client))
        {
            return "ERROR: No estás suscrito a este tema."; // Si no está suscrito, error
        }

        if (clientMessageQueues.ContainsKey(client) && clientMessageQueues[client].ContainsKey(topic))
        {
            if (clientMessageQueues[client][topic].Count > 0)
            {
                string message = clientMessageQueues[client][topic].Dequeue(); // Devuelve el mensaje más antiguo
                Console.WriteLine($"Mensaje enviado al cliente para {topic}: {message}");
                return message;
            }
        }

        return "EMPTY"; // Si no hay mensajes en la cola del cliente
    }


}
