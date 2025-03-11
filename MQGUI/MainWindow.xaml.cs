using System;
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

        private void Subscribe_Click(object sender, RoutedEventArgs e)
        {
            string topic = txtTopic.Text;
            if (!string.IsNullOrEmpty(topic))
            {
                string response = client.SendMessage("SUBSCRIBE", topic);
                lstMessages.Items.Add($"Suscrito a: {topic}");
                lstMessages.Items.Add($"Respuesta del broker: {response}");
            }
            else
            {
                MessageBox.Show("Ingrese un tema válido", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void SendMessage_Click(object sender, RoutedEventArgs e)
        {
            string topic = txtTopic.Text;
            string message = txtMessage.Text;

            if (!string.IsNullOrEmpty(topic) && !string.IsNullOrEmpty(message))
            {
                string response = await Task.Run(() => client.SendMessage("PUBLISH", topic, message));
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
                string response = await Task.Run(() => client.SendMessage("RECEIVE", topic));

                Dispatcher.Invoke(() =>
                {
                    if (!string.IsNullOrEmpty(response))
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


