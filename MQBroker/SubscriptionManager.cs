using System;
using System.Collections.Generic;
using System.Net.Sockets;
using MQClient; // Se asume que esta librería proporciona CustomDictionary, Queue y Nodo.

namespace MQBroker
{
    public class SubscriptionManager
    {
        // Diccionario de tópicos con sus listas de clientes suscritos.
        private CustomDictionary<string, List<TcpClient>> subscribers;
        // Diccionario de clientes con sus colas de mensajes por tópico.
        private CustomDictionary<TcpClient, CustomDictionary<string, Queue>> clientMessageQueues;

        public SubscriptionManager()
        {
            subscribers = new CustomDictionary<string, List<TcpClient>>();
            clientMessageQueues = new CustomDictionary<TcpClient, CustomDictionary<string, Queue>>();
        }

        /// <summary>
        /// Registra la suscripción de un cliente a un tópico.
        /// </summary>
        public string Subscribe(TcpClient client, string appId, string topic)
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
        /// Elimina la suscripción de un cliente en un tópico.
        /// </summary>
        public void Unsubscribe(TcpClient client, string appId, string topic)
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
        /// Publica un mensaje en un tópico hacia todos los clientes suscritos (excepto el emisor).
        /// </summary>
        public void Publish(string topic, string message, TcpClient sender)
        {
            if (subscribers.TryGetValue(topic, out var clients))
            {
                foreach (var client in clients)
                {
                    if (client == sender)
                        continue;

                    if (clientMessageQueues.TryGetValue(client, out var topics) &&
                        topics.TryGetValue(topic, out var queue))
                    {
                        // Se encapsula el mensaje en un Nodo y se encola.
                        queue.Enqueue(new Nodo(message));
                        Console.WriteLine($"📤 Mensaje publicado en {topic} para {client.Client.RemoteEndPoint}");
                    }
                }
            }
        }

        /// <summary>
        /// Recupera el siguiente mensaje de la cola del cliente para un tópico específico.
        /// </summary>
        public string ReceiveMessage(TcpClient client, string appId, string topic)
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
}
