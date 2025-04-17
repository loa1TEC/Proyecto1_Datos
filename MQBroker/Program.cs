using System;

namespace MQBroker
{
    class Program
    {
        static void Main()
        {
            // Se crea e inicia el servidor MQBroker.
            MQBrokerServer server = new MQBrokerServer();
            server.Start();
        }
    }
}
