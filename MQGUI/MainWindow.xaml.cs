using System;
using System.Net.NetworkInformation;
using System.Threading.Tasks;
using System.Windows;
using MQClient;


namespace MQGUI
{
    public partial class MainWindow : Window
    {
        private Client client;

        public MainWindow()
        {
            InitializeComponent();
            client = new Client();
        }

        private async void Subscribe_Click(object sender, RoutedEventArgs e)
        {
            string topic = txtTopic.Text;
            if (!string.IsNullOrEmpty(topic))
            {
                // Enviar la solicitud de suscripción al Broker
                string response = await client.SendMessageAsync("SUBSCRIBE", topic);

                // Mostrar la respuesta en la UI
                MessageBox.Show(response); // Muestra la respuesta del Broker

                lstMessages.Items.Add($"Te has suscrito a {topic}"); // Mostrar en la UI el mensaje de confirmación
                Btndes.Visibility = Visibility.Visible; // Muestra el botón de desuscribirse
            }
            else
            {
                MessageBox.Show("Por favor, ingresa un tema para suscribirte.");
            }
        }

        private async void Unsubscribe_Click(object sender, RoutedEventArgs e)
        {
            string topic = txtTopic.Text;
            if (!string.IsNullOrEmpty(topic))
            {
                // Enviar la solicitud de desuscripción al Broker
                string response = await client.SendMessageAsync("UNSUBSCRIBE", topic);

                // Mostrar la respuesta en la UI
                MessageBox.Show(response);

                lstMessages.Items.Add($"Te has desuscrito de {topic}"); // Mostrar en la UI el mensaje de desuscripción
                Btndes.Visibility = Visibility.Hidden; // Ocultar el botón de desuscribirse
            }
            else
            {
                MessageBox.Show("Por favor, ingresa un tema para desuscribirte.");
            }
        }

        private async void SendMessage_Click(object sender, RoutedEventArgs e)
        {
            string topic = txtTopic.Text;
            string message = txtMessage.Text;

            if (!string.IsNullOrEmpty(topic) && !string.IsNullOrEmpty(message))
            {
                string response = await Task.Run(() => client.SendMessageAsync("PUBLISH", topic, message));
                lstMessages.Items.Add($"Enviado: {message} en {topic}");
                lstMessages.Items.Add($"Respuesta del broker: {response}");
            }
            else
            {
                MessageBox.Show("Ingrese un tema y un mensaje", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void ReceiveMessage_Click(object sender, RoutedEventArgs e)
        {
            string topic = txtTopic.Text;

            if (!string.IsNullOrEmpty(topic))
            {
                // Enviar la solicitud de recepción al Broker
                string response = await client.SendMessageAsync("RECEIVE", topic);

                // Mostrar la respuesta en la UI
                Dispatcher.Invoke(() =>
                {
                    if (!string.IsNullOrEmpty(response) && response != "EMPTY")
                    {
                        lstMessages.Items.Add($"Mensaje recibido en {topic}: {response}");
                    }
                    else
                    {
                        lstMessages.Items.Add($"No hay mensajes nuevos en {topic}.");
                    }
                });
            }
            else
            {
                MessageBox.Show("Ingrese un tema para recibir mensajes", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

    }
}


