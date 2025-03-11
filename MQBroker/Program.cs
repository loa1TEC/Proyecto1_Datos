using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

class Program
{
    // Diccionario para almacenar suscriptores por tema
    private static Dictionary<string, List<TcpClient>> subscribers = new Dictionary<string, List<TcpClient>>();

    static void Main()
    {
        TcpListener server = new TcpListener(IPAddress.Any, 5000);
        server.Start();
        Console.WriteLine("MQBroker esperando conexiones en el puerto 5000...");

        while (true)
        {
            TcpClient client = server.AcceptTcpClient();
            Console.WriteLine("Cliente conectado.");
            Task.Run(() => HandleClient(client)); // Manejar clientes en paralelo
        }
    }

    static void HandleClient(TcpClient client)
    {
        try
        {
            NetworkStream stream = client.GetStream();
            byte[] buffer = new byte[1024];

            while (true)
            {
                int bytesRead = stream.Read(buffer, 0, buffer.Length);
                if (bytesRead == 0) break;

                string receivedMessage = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                Console.WriteLine($"Mensaje recibido: {receivedMessage}");

                string[] parts = receivedMessage.Split('|');
                string command = parts[0];

                if (command == "SUBSCRIBE")
                {
                    string topic = parts[1];
                    Subscribe(client, topic);
                }
                else if (command == "PUBLISH")
                {
                    string topic = parts[1];
                    string message = parts[2];
                    Publish(topic, message);
                }
                else if (command == "RECEIVE")
                {
                    string topic = parts[1];
                    SendLastMessage(client, topic);
                }

            }

            client.Close();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error al manejar cliente: {ex.Message}");
        }
    }


    static void Subscribe(TcpClient client, string topic)
    {
        if (!subscribers.ContainsKey(topic))
        {
            subscribers[topic] = new List<TcpClient>();
        }
        subscribers[topic].Add(client);

        Console.WriteLine($"Cliente suscrito al tema: {topic}");
    }

    private static Dictionary<string, string> lastMessages = new Dictionary<string, string>();

    static void Publish(string topic, string message)
    {
        if (!subscribers.ContainsKey(topic))
        {
            subscribers[topic] = new List<TcpClient>();
        }

        lastMessages[topic] = message; // Guarda el último mensaje del tema

        Console.WriteLine($"Publicando mensaje en {topic}: {message}");
        byte[] data = Encoding.UTF8.GetBytes(message);

        List<TcpClient> failedClients = new List<TcpClient>();

        foreach (var subscriber in subscribers[topic])
        {
            try
            {
                NetworkStream stream = subscriber.GetStream();
                stream.Write(data, 0, data.Length);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error enviando a suscriptor: {ex.Message}");
                failedClients.Add(subscriber);
            }
        }

        // Remover clientes desconectados
        foreach (var failedClient in failedClients)
        {
            subscribers[topic].Remove(failedClient);
        }
    }


    static void SendLastMessage(TcpClient client, string topic)
    {
        if (lastMessages.ContainsKey(topic))
        {
            string lastMessage = lastMessages[topic];
            byte[] data = Encoding.UTF8.GetBytes(lastMessage);

            NetworkStream stream = client.GetStream();
            stream.Write(data, 0, data.Length);
        }
    }



}
